---
name: memorymcp-tokens
description: >-
  Store and maintain abstract token properties in MemoryMCP for cross-memory mesh
  search. Prefer Ref ids (8-char) over Guid in follow-up calls; Guid still works.
  Use when the user asks to save ('spara i minnet', 'kom ihåg'), confirms they want
  a fact stored, or when retrieving/querying memory. Also for listing properties,
  renaming overly specific properties, merging vocabulary, or splitting compound token values.
---

# MemoryMCP token properties

## Ref ids (primary — Guid backward compatible)

Every entity, memory, and token has **Ref** (8-char Base64, **preferred**) and **Id** (Guid, still accepted in all id parameters).

| When | What to do |
|------|------------|
| After `store_memory_bundle` | Use `memoryRef`, `entityRefs`, `tokenRefs` |
| After `find_entities` / search | Cache **Ref** from each item |
| Follow-up tool calls | Pass Ref — not Guid — in `id`, `memoryId`, `entityId`, `tokenId` |
| Batch JSON | `"memoryId":"aB3-xY9z"` — Ref or Guid both work |

Never ask the user for ids. Do not repeat Guid in reasoning when Ref is available. See `get_memorymcp_guide(topic="refs")`.

## When to use memory (before tokens)

| Situation | Action |
|-----------|--------|
| Direct save order: "spara i minnet", "kom ihåg det här" | `store_memory_bundle` immediately |
| Relevant fact, no save request | Ask once: "Vill du att jag ska lägga detta i minnet?" |
| User asks to recall, or task needs prior context | `search_*` — no permission needed |
| Casual chat, nothing worth keeping | Do not use memory tools |

Once saving: extract entities and tokens — never ask "ska jag skapa en entitet?".

Call MCP tool `start_here` if unsure.

Tokens are **abstract search facets** — a mesh layer connecting unrelated memories and entities. They are not a fixed schema; vocabulary emerges as you store and retrieve.

## Core rules

1. **Property = reusable facet**, never entity-specific
   - Good: `Year`, `Name`, `Likes`, `Color`
   - Bad: `EiraBirthYear`, `WineBoffAge`, `LikesFood`

2. **Identity on entities, values on tokens**
   - Entity: *who/what* (Alva, böff, foo)
   - Token: *facet + value* (`Year=1988`, `Name=böff`, `Likes=pasta`)

3. **One token = one atomic value**
   - Two wines → two `Name` tokens + two `Year` tokens
   - Two favorite foods → two `Likes` tokens, not `LikesFood="pasta, hamburgare"`

4. **No locked standard**
   - Discover existing vocabulary with `list_token_properties` before storing
   - Converge naming over time (`BirthYear` → `Year`) when patterns emerge
   - Maintenance is ongoing, not a one-time migration

## Example: two wines in one observation

Raw: *"Jag har två viner, en böff från 1988 och en foo från 2025"*

```json
{
  "raw": "Jag har två viner, en böff från 1988 och en foo från 2025",
  "entitiesJson": "[{\"clientKey\":\"boff\",\"type\":\"Wine\",\"name\":\"böff\"},{\"clientKey\":\"foo\",\"type\":\"Wine\",\"name\":\"foo\"}]",
  "tokensJson": "[{\"property\":\"Name\",\"type\":\"String\",\"stringValue\":\"böff\",\"confidence\":0.95,\"source\":\"Extracted\"},{\"property\":\"Name\",\"type\":\"String\",\"stringValue\":\"foo\",\"confidence\":0.95,\"source\":\"Extracted\"},{\"property\":\"Year\",\"type\":\"Int\",\"intValue\":1988,\"confidence\":0.9,\"source\":\"Extracted\"},{\"property\":\"Year\",\"type\":\"Int\",\"intValue\":2025,\"confidence\":0.9,\"source\":\"Extracted\"}]",
  "entityLinksJson": "[\"boff\",\"foo\"]"
}
```

## Why tokens exist

Enable queries across unrelated data that share a facet:

- *"Which wines were made in my birth year?"* — mesh via `Year`, even if memories never mentioned each other
- *"Who else likes pasta?"* — mesh via `Likes`
- *"Find everything from 1988"* — one property name, many entity types

## Tokens vs relationships — when to use which

Tokens index **directly observed** facet values. Relationships express **how entities connect**. Use both; do not duplicate inferred facts as tokens.

### Decision flow

```
Is the value stated in raw text for this entity?
├─ YES → token on this memory (source=Extracted), entityLinks identify who
│         reuseTokens=true if another memory independently states the same value
└─ NO, but raw compares/links entities ("same age as Alva", "Max's son")
    → relationship (SameAgeAs, ParentOf, …), no copied facet token on the inferred entity
```

### Example: explicit age vs inferred same age

**Memory A** — *"Alva är 38 år"*

```json
{
  "entitiesJson": "[{\"clientKey\":\"alva\",\"type\":\"Person\",\"name\":\"Alva\"}]",
  "tokensJson": "[{\"property\":\"Age\",\"type\":\"Int\",\"intValue\":38,\"confidence\":0.95,\"source\":\"Extracted\"}]",
  "entityLinksJson": "[\"alva\"]"
}
```

**Memory B** — *"Eira är lika gammal som Alva"*

```json
{
  "entitiesJson": "[{\"clientKey\":\"eira\",\"type\":\"Person\",\"name\":\"Eira\"},{\"clientKey\":\"alva\",\"type\":\"Person\",\"name\":\"Alva\"}]",
  "relationshipsJson": "[{\"fromClientKey\":\"eira\",\"toClientKey\":\"alva\",\"relationType\":\"SameAgeAs\",\"confidence\":0.9}]",
  "entityLinksJson": "[\"eira\",\"alva\"]"
}
```

Do **not** add `Age=38` on Eira's memory — that value was not stated for her. Query *"how old is Eira?"* by traversing `SameAgeAs` → Alva → `Age=38`.

### When shared tokens ARE correct

`reuseTokens=true` (default) links multiple memories to one token when each memory **independently** states the same `property+type+value`:

- Memory 1: *"Alva är 38"* → `Age=38`
- Memory 2: *"Leo är också 38"* → reuses same `Age=38` token

Mesh search *"vem är 38?"* returns both memories; `entityLinks` resolve who. This is intentional.

### When NOT to reuse / merge tokens

- Value is **inferred** from another entity (use relationship instead)
- Same number/string would imply **stated** fact but raw only says *"like"*, *"same as"*, *"as old as"*
- **Different provenance** matters: one Extracted, one Derived — do not flatten via shared token
- **Age drifts** — prefer `Year` when birth year is known; set `memoryFrom` when age is time-sensitive

### Entity identity is not a token

Do not create `Name` tokens for people/systems already in `entityLinks`. Identity belongs on the entity record. `Name` tokens are for facet values in raw text (wine name `böff`), not disambiguation.

## Ongoing maintenance workflow

Run this lightly whenever you **store** or **retrieve** memories and notice messy properties.

### 1. Discover vocabulary

```
list_token_properties
list_tokens_by_property
list_tokens_by_property(maxMemoryLinks=1)   # rare one-off tokens
```

Look for entity-prefixed or compound names (`EiraBirthYear`, `LikesFood`).

### 2. Rename overly specific properties

```
rename_token_property(fromProperty="EiraBirthYear", toProperty="Year", preview=true)
rename_token_property(fromProperty="EiraBirthYear", toProperty="Year")
```

Or merge several at once:

```
merge_token_properties(
  fromPropertiesJson='["EiraBirthYear","MaxBirthYear","BirthYear"]',
  toProperty="Year",
  preview=true
)
```

### 3. Split compound values

When one token holds multiple values:

```
split_token_value(tokenId="<Ref from list_tokens_by_property>", targetProperty="Likes", preview=true)
split_token_value(tokenId="<Ref>", targetProperty="Likes")
```

### 4. Fix at store time

Prefer correct abstract tokens in new `store_memory_bundle` calls rather than only cleaning up later.

### 5. Do not over-merge during maintenance

Merging duplicate tokens (same property+type+value) helps mesh search **only when both memories independently stated that value**. If one memory inferred the value via a relationship, unlink the inferred memory's token link instead of consolidating — then add or keep the relationship.

## When to split memories vs. multiple tokens

- **Multiple tokens in one memory** — fine when raw text describes several subjects together (wines example)
- **One memory per subject** — clearer when each fact needs an unambiguous entity link
- Either way: keep property names abstract

## Tools reference

| Tool | Purpose |
|------|---------|
| `list_token_properties` | Quick catalog of property names + counts |
| `list_tokens_by_property` | Tokens per property with memory link counts (popular vs rare) |
| `rename_token_property` | Rename one property across all active tokens |
| `merge_token_properties` | Rename several properties into one canonical name |
| `split_token_value` | Split `a, b, c` string into separate atomic tokens |
| `find_tokens` / `search_memories_by_token` / `search_entities_by_token` | Query the mesh |
| `store_memory_bundle` | Store raw + entities + tokens; response has `memoryRef`, `entityRefs`, `tokenRefs` |

Always use `preview=true` first when unsure about impact. Prefer **Ref** over Guid in all follow-up id parameters.
