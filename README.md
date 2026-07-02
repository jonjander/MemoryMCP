# MemoryMCP

Local MCP server for long-term AI memory backed by EF Core. Supports **SQL Server** (default) or **SQLite** (`--typ sqlite`) for zero-config and offline portable deployment.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- **SQL Server** (default) — database `MemoryMCP` is created automatically on first run via migrations
- **SQLite** (optional) — no external database; use `--typ sqlite` to create `memory.db` next to the executable

## Database modes

### SQL Server (default)

Requires a connection string in `appsettings.json` or via environment variable.

### SQLite (portable / offline)

No connection string needed. Creates and uses `memory.db` in the same folder as the executable/DLL:

```bash
dotnet run -- --typ sqlite
```

Or via environment variable (useful in Cursor MCP config):

```bash
MEMORYMCP_TYP=sqlite dotnet MemoryMCP.dll
```

Text search uses `LIKE` on SQLite (no SQL Server full-text search). All other features work the same.

## Portable offline publish

Build on a machine **with internet**, then copy the publish folder to an offline Linux Docker (or Windows) host that has .NET 10:

```powershell
.\scripts\publish-portable.ps1
```

This produces `dist/linux-x64/` and `dist/win-x64/`. On the target host:

```bash
cd /app
dotnet MemoryMCP.dll --typ sqlite
dotnet MemoryMCP.dll --typ sqlite --verify
```

Optional Docker image that only copies pre-built output (no restore in container):

```powershell
.\scripts\publish-portable.ps1 -Runtime linux-x64
docker build -f Dockerfile.offline -t memorymcp .
```

Do **not** run `dotnet build` or `dotnet run` from source in the offline container — that requires NuGet access.

### SQL Server with Docker

```powershell
docker run --name "sql" -p 1433:1433 -v c:\docker\sql:/var/opt/mssql/data -e "MSSQL_SA_PASSWORD=Lösen0rd" -e "ACCEPT_EULA=Y" -d mcr.microsoft.com/mssql/server:2022-latest
```

Data is persisted under `c:\docker\sql`. Use the same password in `appsettings.json` below.

- Connection string in `appsettings.json`:

```json
"ConnectionStrings": {
  "MemoryMCP": "Server=127.0.0.1;User Id=SA;Password=Lösen0rd;Database=MemoryMCP;TrustServerCertificate=True"
}
```

## Build and run

```bash
dotnet restore
dotnet build
dotnet run
```

Run automated smoke verification:

```bash
dotnet run -- --verify                  # SQL Server (default)
dotnet run -- --typ sqlite --verify     # SQLite
```

The server uses **stdio** transport for MCP clients such as Cursor.

## Cursor MCP configuration

Use the dedicated MCP output folder so rebuilds work while Cursor has the server running:

```json
{
  "mcpServers": {
    "memorymcp": {
      "command": "dotnet",
      "args": ["C:\\Git\\MemoryMCP\\bin\\mcp\\MemoryMCP.dll"]
    }
  }
}
```

A ready-made snippet for your **global** Cursor config (`%USERPROFILE%\.cursor\mcp.json`):

```json
"memorymcp": {
  "command": "dotnet",
  "args": ["C:\\Git\\MemoryMCP\\bin\\mcp\\MemoryMCP.dll", "--typ", "sqlite"]
}
```

For portable publish output:

```json
"memorymcp": {
  "command": "dotnet",
  "args": ["C:\\Git\\MemoryMCP\\dist\\win-x64\\MemoryMCP.dll", "--typ", "sqlite"]
}
```

Keep `memorymcp` only in the global config if you already use that for other servers (glider, mudblazor, etc.). Avoid duplicating the same server name in a project-local `.cursor/mcp.json` — Cursor can merge/override and you may end up running an old entry.

### After code changes

**Refresh in Cursor is not enough** — the MCP server process must restart with a new build.

1. Turn off `memorymcp` in Cursor Settings → MCP (or click **Restart** on the server)
2. Rebuild:
   ```powershell
   .\scripts\rebuild-mcp.ps1
   ```
   Or manually: `dotnet build MemoryMCP.csproj -o bin/mcp`
3. Turn on / restart `memorymcp` in Cursor

Verify tool count (should be **42**):
```powershell
dotnet exec bin/mcp/MemoryMCP.dll -- --list-tools
```

If `dotnet build` fails with "file is locked by MemoryMCP", Cursor is still running the old server — disable it first.

## Agent guidance

New agents should not guess tool order. MemoryMCP exposes guidance in three ways:

1. **Server instructions** — sent automatically when the MCP session starts (workflow summary).
2. **`get_memorymcp_guide`** — call with optional `topic`: `overview`, `quickstart`, `store`, `retrieve`, `tokens`, `maintenance`, `examples`.
3. **MCP resources** (markdown): `memorymcp://guide/workflow`, `memorymcp://guide/tokens`, `memorymcp://guide/examples`.

`store_memory_bundle` and `create_memory` responses include a `nextSteps` array with suggested follow-up actions.

See `.cursor/skills/memorymcp-tokens/SKILL.md` for extended Cursor skill guidance.

## MCP tools

### Guide
- `get_memorymcp_guide` – workflow and usage guide (call first when unsure)

### Memories
- `create_memory` – store raw observation (raw text is immutable)
- `get_memory` – get memory with linked data and revision history
- `list_memories` – paginated list (active only by default)
- `update_memory_from` – set/correct when the observation occurred
- `invalidate_memory` – mark as invalid or retracted without deleting raw text
- `revise_memory` – create a corrected successor memory; original preserved as Superseded
- `get_memory_history` – audit log and full correction chain

### Entities
- `create_entity` – create or resolve entity
- `update_entity` – fix name/type (e.g. misspellings)
- `merge_entities` – merge duplicate entities into one
- `deprecate_entity` – mark entity inactive
- `find_entities` – search by name/type
- `link_memory_entity` / `unlink_memory_entity`
- `get_entity` – entity details with revisions
- `get_entity_history` – audit log

### Tokens
- `create_token` – one atomic structured fact with confidence
- `update_token` – fix property/value in place (all linked memories)
- `supersede_token` – corrected successor token with relinking
- `deprecate_token` – mark token inactive
- `link_memory_token` / `unlink_memory_token`
- `link_memory_tokens` – batch link existing tokens to memories (max 500)
- `create_and_link_tokens` – batch create tokens and link to memories (max 500)
- `get_token` – token details with linked memories
- `get_token_history` – audit log and correction chain
- `find_tokens` – search tokens by abstract property and value
- `list_token_properties` – catalog of distinct property names
- `list_tokens_by_property` – tokens grouped by property with usage counts
- `rename_token_property` – rename a property across all active tokens
- `merge_token_properties` – merge several property names into one
- `split_token_value` – split compound string values into atomic tokens

### Relationships
- `create_relationship` – link two entities
- `find_relationships` – filter relationships
- `get_entity_graph` – entity knowledge graph

### Search
- `search_memories_by_text`
- `search_memories_by_entity`
- `search_memories_by_token`
- `search_entities_by_token`

### Bundle
- `store_memory_bundle` – transactional store of raw + entities + tokens + relationships
- `store_memory_bundles` – batch store up to 100 bundles in one transaction

## Token properties (abstract mesh facets)

Tokens are **not** entity-specific fields. Property names are reusable facets for fast search and cross-memory mesh queries — e.g. find wines from the same year as your birthday even when those memories never mentioned each other.

| Do | Don't |
|----|-------|
| `Year`, `Name`, `Likes`, `Color` | `SofiaBirthYear`, `WineBoffAge`, `LikesFood` |
| Two `Name` + two `Year` for two wines | `BoffName` + `FooName` |
| `Likes=pasta` and `Likes=hamburgare` | `LikesFood="pasta, hamburgare"` |

Identity lives on **entities**; atomic values live on **tokens**. There is no fixed property schema — discover and converge vocabulary over time via `list_token_properties`.

See `.cursor/skills/memorymcp-tokens/SKILL.md` for agent guidance and maintenance workflows.

## Example bundle (single subject)

```json
{
  "raw": "Maja is 15 years old today. It is 2026.",
  "entitiesJson": "[{\"clientKey\":\"maja\",\"type\":\"Person\",\"name\":\"Maja\"}]",
  "tokensJson": "[{\"property\":\"Age\",\"type\":\"Int\",\"intValue\":15,\"confidence\":0.95,\"source\":\"Extracted\"},{\"property\":\"Year\",\"type\":\"Int\",\"intValue\":2011,\"confidence\":0.75,\"source\":\"Derived\"}]",
  "entityLinksJson": "[\"maja\"]"
}
```

## Example bundle (multiple subjects, shared properties)

```json
{
  "raw": "Jag har två viner, en böff från 1988 och en foo från 2025",
  "entitiesJson": "[{\"clientKey\":\"boff\",\"type\":\"Wine\",\"name\":\"böff\"},{\"clientKey\":\"foo\",\"type\":\"Wine\",\"name\":\"foo\"}]",
  "tokensJson": "[{\"property\":\"Name\",\"type\":\"String\",\"stringValue\":\"böff\",\"confidence\":0.95,\"source\":\"Extracted\"},{\"property\":\"Name\",\"type\":\"String\",\"stringValue\":\"foo\",\"confidence\":0.95,\"source\":\"Extracted\"},{\"property\":\"Year\",\"type\":\"Int\",\"intValue\":1988,\"confidence\":0.9,\"source\":\"Extracted\"},{\"property\":\"Year\",\"type\":\"Int\",\"intValue\":2025,\"confidence\":0.9,\"source\":\"Extracted\"}]",
  "entityLinksJson": "[\"boff\",\"foo\"]"
}
```

## Token maintenance (ongoing)

When storing or retrieving memories, check for overly specific properties and clean up:

1. `list_token_properties` — quick vocabulary overview
2. `list_tokens_by_property` — grouped tokens with memory link counts; use `maxMemoryLinks=1` to find one-off values
3. `rename_token_property` / `merge_token_properties` — converge names (use `preview=true` first)
4. `split_token_value` — split compound values like `LikesFood="pasta, hamburgare"` into separate `Likes` tokens

This is expected housekeeping, not a one-time migration.

## Database migrations

Migrations run automatically on startup. SQL Server and SQLite use separate migration chains:

```bash
# SQL Server
dotnet ef migrations add <Name> --context MemoryDbContext

# SQLite
dotnet ef migrations add <Name> --context SqliteMemoryDbContext --output-dir Migrations/Sqlite
```

When changing the schema, add migrations for **both** contexts.

## Design rules

1. `Memory.Raw` is immutable after creation — corrections create a new memory
2. Use `update_memory_from` when you learn the exact observation date
3. Use `invalidate_memory` when a memory should no longer be trusted
4. Use `revise_memory` when the content needs correction; the original stays in the database
5. Entities are deduplicated by `(Type, Name)`
6. Token properties are abstract and dynamic — no schema migrations; maintain vocabulary over time
7. All extracted knowledge stores confidence values
8. Relationships support any entity types (people, items, places, etc.)
9. Search excludes inactive memories by default
