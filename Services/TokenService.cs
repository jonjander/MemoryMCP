using MemoryMCP.Data;
using MemoryMCP.Models;
using Microsoft.EntityFrameworkCore;

namespace MemoryMCP.Services;

public class TokenService(MemoryDbContext db)
{
    public async Task<TokenSummaryDto> CreateAsync(
        string property,
        PropertyType type,
        int? intValue = null,
        bool? boolValue = null,
        string? stringValue = null,
        float? floatValue = null,
        DateTime? dateTimeValue = null,
        float confidence = 1f,
        TokenSource source = TokenSource.Extracted,
        CancellationToken cancellationToken = default)
    {
        return TokenValueHelper.ToSummary(await CreateAsync(new BundleTokenInput(
            property, type, intValue, boolValue, stringValue, floatValue, dateTimeValue, confidence, source), cancellationToken));
    }

    public async Task<Token> CreateAsync(BundleTokenInput input, CancellationToken cancellationToken = default)
    {
        var token = new Token
        {
            Id = Guid.NewGuid(),
            Created = DateTime.UtcNow,
            Property = input.Property.Trim(),
            Confidence = input.Confidence,
            Source = input.Source,
            Status = TokenStatus.Active
        };

        TokenValueHelper.ApplyValues(token, input.Type, input.IntValue, input.BoolValue, input.StringValue, input.FloatValue, input.DateTimeValue);
        db.Tokens.Add(token);
        await db.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<Token> FindOrCreateAsync(BundleTokenInput input, CancellationToken cancellationToken = default)
    {
        var searchValue = TokenValueHelper.ComputeSearchValue(
            input.Type, input.IntValue, input.BoolValue, input.StringValue, input.FloatValue, input.DateTimeValue);

        var existing = await db.Tokens
            .Where(t => t.Status == TokenStatus.Active)
            .FirstOrDefaultAsync(t =>
                t.Property == input.Property.Trim() &&
                t.Type == input.Type &&
                t.SearchValue == searchValue,
                cancellationToken);

        if (existing is not null)
            return existing;

        var token = new Token
        {
            Id = Guid.NewGuid(),
            Created = DateTime.UtcNow,
            Property = input.Property.Trim(),
            Confidence = input.Confidence,
            Source = input.Source,
            Status = TokenStatus.Active
        };

        TokenValueHelper.ApplyValues(token, input.Type, input.IntValue, input.BoolValue, input.StringValue, input.FloatValue, input.DateTimeValue);
        db.Tokens.Add(token);
        return token;
    }

    public async Task<TokenSummaryDto> UpdateTokenAsync(
        Guid id,
        string? property = null,
        PropertyType? type = null,
        int? intValue = null,
        bool? boolValue = null,
        string? stringValue = null,
        float? floatValue = null,
        DateTime? dateTimeValue = null,
        float? confidence = null,
        TokenSource? source = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var token = await db.Tokens.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Token not found.");

        EnsureActive(token);

        var previousProperty = token.Property;
        var previousType = token.Type;
        var previousSearchValue = token.SearchValue;
        var previousConfidence = token.Confidence;
        var previousSource = token.Source;

        if (!string.IsNullOrWhiteSpace(property))
            token.Property = property.Trim();

        if (confidence.HasValue)
            token.Confidence = confidence.Value;

        if (source.HasValue)
            token.Source = source.Value;

        var valueType = type ?? token.Type;
        var hasValueUpdate = type.HasValue || intValue.HasValue || boolValue.HasValue ||
                             stringValue is not null || floatValue.HasValue || dateTimeValue.HasValue;

        if (hasValueUpdate)
        {
            var nextInt = type.HasValue ? intValue : token.IntValue;
            var nextBool = type.HasValue ? boolValue : token.BoolValue;
            var nextString = type.HasValue ? stringValue : token.StringValue;
            var nextFloat = type.HasValue ? floatValue : token.FloatValue;
            var nextDateTime = type.HasValue ? dateTimeValue : token.DateTimeValue;

            if (type.HasValue)
                TokenValueHelper.ApplyValues(token, valueType, intValue, boolValue, stringValue, floatValue, dateTimeValue);
            else
                TokenValueHelper.ApplyValues(token, token.Type, nextInt, nextBool, nextString, nextFloat, nextDateTime);
        }

        token.Updated = DateTime.UtcNow;

        if (!string.Equals(previousProperty, token.Property, StringComparison.Ordinal))
            await AddRevisionAsync(token, TokenRevisionType.PropertyUpdated, note, previousProperty, token.Property, previousType, token.Type, previousSearchValue, token.SearchValue, previousConfidence, token.Confidence, previousSource, token.Source, token.Status, token.Status, null, cancellationToken);

        if (type.HasValue && previousType != token.Type)
            await AddRevisionAsync(token, TokenRevisionType.TypeUpdated, note, previousProperty, token.Property, previousType, token.Type, previousSearchValue, token.SearchValue, previousConfidence, token.Confidence, previousSource, token.Source, token.Status, token.Status, null, cancellationToken);

        if (hasValueUpdate && previousSearchValue != token.SearchValue)
            await AddRevisionAsync(token, TokenRevisionType.ValueUpdated, note, previousProperty, token.Property, previousType, token.Type, previousSearchValue, token.SearchValue, previousConfidence, token.Confidence, previousSource, token.Source, token.Status, token.Status, null, cancellationToken);

        if (confidence.HasValue && Math.Abs(previousConfidence - token.Confidence) > float.Epsilon)
            await AddRevisionAsync(token, TokenRevisionType.ConfidenceUpdated, note, previousProperty, token.Property, previousType, token.Type, previousSearchValue, token.SearchValue, previousConfidence, token.Confidence, previousSource, token.Source, token.Status, token.Status, null, cancellationToken);

        if (source.HasValue && previousSource != token.Source)
            await AddRevisionAsync(token, TokenRevisionType.SourceUpdated, note, previousProperty, token.Property, previousType, token.Type, previousSearchValue, token.SearchValue, previousConfidence, token.Confidence, previousSource, token.Source, token.Status, token.Status, null, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return TokenValueHelper.ToSummary(token);
    }

    public async Task<SupersedeTokenResultDto> SupersedeTokenAsync(
        Guid originalTokenId,
        string? property = null,
        PropertyType? type = null,
        int? intValue = null,
        bool? boolValue = null,
        string? stringValue = null,
        float? floatValue = null,
        DateTime? dateTimeValue = null,
        float? confidence = null,
        TokenSource? source = null,
        Guid? memoryId = null,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var original = await db.Tokens
            .Include(t => t.Memories)
            .FirstOrDefaultAsync(t => t.Id == originalTokenId, cancellationToken)
            ?? throw new InvalidOperationException("Token not found.");

        if (original.Status == TokenStatus.Superseded)
            throw new InvalidOperationException("Token is already superseded.");

        var successorType = type ?? original.Type;
        var successor = new Token
        {
            Id = Guid.NewGuid(),
            Created = DateTime.UtcNow,
            Property = (property ?? original.Property).Trim(),
            Confidence = confidence ?? original.Confidence,
            Source = source ?? original.Source,
            Status = TokenStatus.Active,
            SupersedesTokenId = original.Id
        };

        TokenValueHelper.ApplyValues(
            successor,
            successorType,
            intValue ?? original.IntValue,
            boolValue ?? original.BoolValue,
            stringValue ?? original.StringValue,
            floatValue ?? original.FloatValue,
            dateTimeValue ?? original.DateTimeValue);

        var links = memoryId.HasValue
            ? original.Memories.Where(m => m.MemoryId == memoryId.Value).ToList()
            : original.Memories.ToList();

        if (links.Count == 0)
            throw new InvalidOperationException(memoryId.HasValue
                ? "Token is not linked to the specified memory."
                : "Token has no memory links to supersede.");

        db.Tokens.Add(successor);

        var relinked = new List<Guid>();
        foreach (var link in links)
        {
            var exists = await db.MemoryTokens.AnyAsync(mt => mt.MemoryId == link.MemoryId && mt.TokenId == successor.Id, cancellationToken);
            if (!exists)
            {
                db.MemoryTokens.Add(new MemoryToken
                {
                    Id = Guid.NewGuid(),
                    MemoryId = link.MemoryId,
                    TokenId = successor.Id
                });
            }

            db.MemoryTokens.Remove(link);
            relinked.Add(link.MemoryId);
        }

        if (!memoryId.HasValue)
        {
            original.Status = TokenStatus.Superseded;
            original.SupersededByTokenId = successor.Id;
        }

        original.Updated = DateTime.UtcNow;

        await AddRevisionAsync(original, TokenRevisionType.Superseded, note, original.Property, successor.Property, original.Type, successor.Type, original.SearchValue, successor.SearchValue, original.Confidence, successor.Confidence, original.Source, successor.Source, TokenStatus.Active, TokenStatus.Superseded, successor.Id, cancellationToken);
        await AddRevisionAsync(successor, TokenRevisionType.ValueUpdated, note, null, successor.Property, null, successor.Type, null, successor.SearchValue, null, successor.Confidence, null, successor.Source, null, TokenStatus.Active, null, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);

        return new SupersedeTokenResultDto(
            original.Id,
            successor.Id,
            TokenValueHelper.ToSummary(original),
            TokenValueHelper.ToSummary(successor),
            relinked);
    }

    public async Task<TokenSummaryDto> DeprecateTokenAsync(Guid id, string? note = null, CancellationToken cancellationToken = default)
    {
        var token = await db.Tokens.FirstOrDefaultAsync(t => t.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Token not found.");

        EnsureActive(token);

        var previousStatus = token.Status;
        token.Status = TokenStatus.Deprecated;
        token.Updated = DateTime.UtcNow;

        await AddRevisionAsync(token, TokenRevisionType.Deprecated, note, token.Property, token.Property, token.Type, token.Type, token.SearchValue, token.SearchValue, token.Confidence, token.Confidence, token.Source, token.Source, previousStatus, token.Status, null, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return TokenValueHelper.ToSummary(token);
    }

    public async Task<TokenDetailDto?> GetTokenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var token = await db.Tokens
            .AsNoTracking()
            .Include(t => t.Revisions)
            .Include(t => t.SupersedesToken)
            .Include(t => t.SupersededByToken)
            .Include(t => t.Memories)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (token is null)
            return null;

        return new TokenDetailDto(
            TokenValueHelper.ToSummary(token),
            token.Created,
            token.Updated,
            token.Memories.Select(m => m.MemoryId).ToList(),
            token.Revisions.OrderByDescending(r => r.Created).Select(ToRevisionDto).ToList(),
            token.SupersedesToken is null ? null : TokenValueHelper.ToSummary(token.SupersedesToken),
            token.SupersededByToken is null ? null : TokenValueHelper.ToSummary(token.SupersededByToken));
    }

    public async Task<TokenHistoryDto?> GetTokenHistoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var token = await db.Tokens.AsNoTracking().Include(t => t.Revisions).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (token is null)
            return null;

        var chain = new List<TokenSummaryDto> { TokenValueHelper.ToSummary(token) };
        var current = token;

        while (current.SupersedesTokenId.HasValue)
        {
            var predecessor = await db.Tokens.AsNoTracking().FirstOrDefaultAsync(t => t.Id == current.SupersedesTokenId.Value, cancellationToken);
            if (predecessor is null)
                break;
            chain.Insert(0, TokenValueHelper.ToSummary(predecessor));
            current = predecessor;
        }

        current = token;
        while (current.SupersededByTokenId.HasValue)
        {
            var successor = await db.Tokens.AsNoTracking().FirstOrDefaultAsync(t => t.Id == current.SupersededByTokenId.Value, cancellationToken);
            if (successor is null)
                break;
            chain.Add(TokenValueHelper.ToSummary(successor));
            current = successor;
        }

        return new TokenHistoryDto(
            TokenValueHelper.ToSummary(token),
            token.Revisions.OrderByDescending(r => r.Created).Select(ToRevisionDto).ToList(),
            chain);
    }

    public async Task<IReadOnlyList<TokenSummaryDto>> FindTokensAsync(
        string? property = null,
        PropertyType? type = null,
        int? intValue = null,
        bool? boolValue = null,
        string? stringValue = null,
        float? floatValue = null,
        DateTime? dateTimeValue = null,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Tokens.AsNoTracking().AsQueryable();
        if (!includeInactive)
            query = query.Where(t => t.Status == TokenStatus.Active);

        if (!string.IsNullOrWhiteSpace(property))
            query = query.Where(t => t.Property == property);

        if (type.HasValue)
            query = query.Where(t => t.Type == type.Value);

        if (intValue.HasValue)
            query = query.Where(t => t.IntValue == intValue.Value);

        if (boolValue.HasValue)
            query = query.Where(t => t.BoolValue == boolValue.Value);

        if (!string.IsNullOrWhiteSpace(stringValue))
        {
            var sv = stringValue.Trim().ToLowerInvariant();
            query = query.Where(t => t.SearchValue == sv || t.StringValue == stringValue);
        }

        if (floatValue.HasValue)
            query = query.Where(t => t.FloatValue == floatValue.Value);

        if (dateTimeValue.HasValue)
            query = query.Where(t => t.DateTimeValue == dateTimeValue.Value);

        var tokens = await query.OrderByDescending(t => t.Created).Take(100).ToListAsync(cancellationToken);
        return tokens.Select(TokenValueHelper.ToSummary).ToList();
    }

    public async Task<IReadOnlyList<PropertyCatalogEntryDto>> ListPropertiesAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var query = db.Tokens.AsNoTracking().AsQueryable();
        if (!includeInactive)
            query = query.Where(t => t.Status == TokenStatus.Active);

        var tokens = await query.ToListAsync(cancellationToken);

        return tokens
            .GroupBy(t => t.Property, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new PropertyCatalogEntryDto(
                g.Key,
                g.Count(t => t.Status == TokenStatus.Active),
                g.Count(),
                g.Select(t => t.Type).Distinct().OrderBy(t => t).ToList(),
                g.Where(t => t.Status == TokenStatus.Active)
                    .Select(TokenValueHelper.FormatDisplayValue)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(5)
                    .ToList()))
            .ToList();
    }

    public async Task<IReadOnlyList<PropertyTokenGroupDto>> ListTokensByPropertyAsync(
        bool includeInactive = false,
        string? property = null,
        int maxTokensPerProperty = 25,
        int? maxMemoryLinks = null,
        CancellationToken cancellationToken = default)
    {
        if (maxTokensPerProperty < 1)
            maxTokensPerProperty = 25;

        var tokenQuery = db.Tokens.AsNoTracking().AsQueryable();
        if (!includeInactive)
            tokenQuery = tokenQuery.Where(t => t.Status == TokenStatus.Active);

        if (!string.IsNullOrWhiteSpace(property))
            tokenQuery = tokenQuery.Where(t => t.Property == property.Trim());

        var linkCounts = await db.MemoryTokens
            .AsNoTracking()
            .GroupBy(mt => mt.TokenId)
            .Select(g => new { TokenId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TokenId, x => x.Count, cancellationToken);

        var tokens = await tokenQuery.ToListAsync(cancellationToken);

        return tokens
            .GroupBy(t => t.Property, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var tokenEntries = g
                    .Select(t => new TokenUsageEntryDto(
                        t.Ref ?? string.Empty,
                        t.Id,
                        TokenValueHelper.FormatDisplayValue(t),
                        t.Type,
                        linkCounts.GetValueOrDefault(t.Id),
                        t.Status))
                    .Where(e => !maxMemoryLinks.HasValue || e.MemoryLinkCount <= maxMemoryLinks.Value)
                    .OrderBy(e => e.MemoryLinkCount)
                    .ThenBy(e => e.DisplayValue, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                var totalMemoryLinks = g.Sum(t => linkCounts.GetValueOrDefault(t.Id));

                return new PropertyTokenGroupDto(
                    g.Key,
                    g.Count(t => t.Status == TokenStatus.Active),
                    g.Count(),
                    totalMemoryLinks,
                    g.Select(t => t.Type).Distinct().OrderBy(t => t).ToList(),
                    tokenEntries.Take(maxTokensPerProperty).ToList());
            })
            .Where(g => g.Tokens.Count > 0 || !maxMemoryLinks.HasValue)
            .OrderByDescending(g => g.TotalMemoryLinks)
            .ThenByDescending(g => g.ActiveTokenCount)
            .ThenBy(g => g.Property, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<RenamePropertyResultDto> RenamePropertyAsync(
        string fromProperty,
        string toProperty,
        string? note = null,
        bool preview = false,
        CancellationToken cancellationToken = default)
    {
        fromProperty = fromProperty.Trim();
        toProperty = toProperty.Trim();

        if (string.IsNullOrWhiteSpace(fromProperty) || string.IsNullOrWhiteSpace(toProperty))
            throw new InvalidOperationException("Property names are required.");

        if (string.Equals(fromProperty, toProperty, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("From and to property are the same.");

        var tokens = await db.Tokens
            .Where(t => t.Status == TokenStatus.Active && t.Property == fromProperty)
            .ToListAsync(cancellationToken);

        if (preview)
        {
            return new RenamePropertyResultDto(
                fromProperty,
                toProperty,
                tokens.Count,
                tokens.Select(t => t.Id).ToList(),
                Preview: true);
        }

        var revisionNote = note ?? $"Renamed property {fromProperty} → {toProperty}.";
        foreach (var token in tokens)
        {
            var previousProperty = token.Property;
            token.Property = toProperty;
            token.Updated = DateTime.UtcNow;

            await AddRevisionAsync(
                token,
                TokenRevisionType.PropertyUpdated,
                revisionNote,
                previousProperty,
                toProperty,
                token.Type,
                token.Type,
                token.SearchValue,
                token.SearchValue,
                token.Confidence,
                token.Confidence,
                token.Source,
                token.Source,
                token.Status,
                token.Status,
                null,
                cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);

        return new RenamePropertyResultDto(
            fromProperty,
            toProperty,
            tokens.Count,
            tokens.Select(t => t.Id).ToList());
    }

    public async Task<MergePropertiesResultDto> MergePropertiesAsync(
        IReadOnlyList<string> fromProperties,
        string toProperty,
        string? note = null,
        bool preview = false,
        CancellationToken cancellationToken = default)
    {
        if (fromProperties.Count == 0)
            throw new InvalidOperationException("At least one source property is required.");

        var distinctSources = fromProperties
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(p => !string.Equals(p, toProperty.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (distinctSources.Count == 0)
            throw new InvalidOperationException("No source properties differ from the target property.");

        var renames = new List<RenamePropertyResultDto>();
        foreach (var source in distinctSources)
        {
            var result = await RenamePropertyAsync(
                source,
                toProperty.Trim(),
                note ?? $"Merged property {source} → {toProperty.Trim()}.",
                preview,
                cancellationToken);
            renames.Add(result);
        }

        return new MergePropertiesResultDto(
            toProperty.Trim(),
            renames,
            renames.Sum(r => r.TokensUpdated),
            preview);
    }

    public async Task<SplitTokenResultDto> SplitTokenValueAsync(
        Guid tokenId,
        string targetProperty,
        string delimiter = ",",
        string? note = null,
        bool preview = false,
        CancellationToken cancellationToken = default)
    {
        targetProperty = targetProperty.Trim();
        if (string.IsNullOrWhiteSpace(targetProperty))
            throw new InvalidOperationException("Target property is required.");

        var token = await db.Tokens
            .Include(t => t.Memories)
            .FirstOrDefaultAsync(t => t.Id == tokenId, cancellationToken)
            ?? throw new InvalidOperationException("Token not found.");

        EnsureActive(token);

        if (token.Type != PropertyType.String || string.IsNullOrWhiteSpace(token.StringValue))
            throw new InvalidOperationException("Only active string tokens with a value can be split.");

        var parts = token.StringValue
            .Split([delimiter], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (parts.Count < 2)
            throw new InvalidOperationException("Token value does not contain multiple parts to split.");

        if (preview)
        {
            return new SplitTokenResultDto(
                token.Id,
                token.Property,
                targetProperty,
                parts,
                [],
                null,
                Preview: true);
        }

        var memoryIds = token.Memories.Select(m => m.MemoryId).ToList();
        var newTokens = new List<Token>();

        foreach (var part in parts)
        {
            var newToken = await FindOrCreateAsync(new BundleTokenInput(
                targetProperty,
                PropertyType.String,
                StringValue: part,
                Confidence: token.Confidence,
                Source: token.Source), cancellationToken);

            newTokens.Add(newToken);

            foreach (var memoryId in memoryIds)
            {
                var exists = await db.MemoryTokens.AnyAsync(
                    mt => mt.MemoryId == memoryId && mt.TokenId == newToken.Id,
                    cancellationToken);

                if (!exists)
                {
                    db.MemoryTokens.Add(new MemoryToken
                    {
                        Id = Guid.NewGuid(),
                        MemoryId = memoryId,
                        TokenId = newToken.Id
                    });
                }
            }
        }

        foreach (var link in token.Memories.ToList())
            db.MemoryTokens.Remove(link);

        await db.SaveChangesAsync(cancellationToken);

        var deprecated = await DeprecateTokenAsync(
            token.Id,
            note ?? $"Split into {parts.Count} {targetProperty} token(s).",
            cancellationToken);

        return new SplitTokenResultDto(
            token.Id,
            token.Property,
            targetProperty,
            parts,
            newTokens.Select(TokenValueHelper.ToSummary).ToList(),
            deprecated);
    }

    private static void EnsureActive(Token token)
    {
        if (token.Status != TokenStatus.Active)
            throw new InvalidOperationException($"Token is {token.Status} and cannot be modified.");
    }

    private async Task AddRevisionAsync(
        Token token,
        TokenRevisionType revisionType,
        string? note,
        string? previousProperty,
        string? newProperty,
        PropertyType? previousType,
        PropertyType? newType,
        string? previousSearchValue,
        string? newSearchValue,
        float? previousConfidence,
        float? newConfidence,
        TokenSource? previousSource,
        TokenSource? newSource,
        TokenStatus? previousStatus,
        TokenStatus? newStatus,
        Guid? successorTokenId,
        CancellationToken cancellationToken)
    {
        db.TokenRevisions.Add(new TokenRevision
        {
            Id = Guid.NewGuid(),
            TokenId = token.Id,
            RevisionType = revisionType,
            Created = DateTime.UtcNow,
            Note = note,
            PreviousProperty = previousProperty,
            NewProperty = newProperty,
            PreviousType = previousType,
            NewType = newType,
            PreviousSearchValue = previousSearchValue,
            NewSearchValue = newSearchValue,
            PreviousConfidence = previousConfidence,
            NewConfidence = newConfidence,
            PreviousSource = previousSource,
            NewSource = newSource,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            SuccessorTokenId = successorTokenId
        });
        await Task.CompletedTask;
    }

    private static TokenRevisionDto ToRevisionDto(TokenRevision revision) =>
        new(
            revision.Id,
            revision.RevisionType,
            revision.Created,
            revision.Note,
            revision.PreviousProperty,
            revision.NewProperty,
            revision.PreviousType,
            revision.NewType,
            revision.PreviousSearchValue,
            revision.NewSearchValue,
            revision.PreviousConfidence,
            revision.NewConfidence,
            revision.PreviousSource,
            revision.NewSource,
            revision.PreviousStatus,
            revision.NewStatus,
            revision.SuccessorTokenId);
}
