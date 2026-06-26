namespace MemoryMCP;

/// <summary>
/// Agent workflow and usage guidance exposed via server instructions, MCP resources, and the guide tool.
/// </summary>
public static class AgentGuidance
{
  public const string ServerInstructions =
      "MemoryMCP is long-term memory — use only when the user wants to save or when you need to recall. " +
      "Ids: prefer short Ref (8 chars) from responses in follow-up calls; Guid still works everywhere for backward compatibility. " +
      "Save: explicit orders ('spara i minnet', 'kom ihåg det här') → store_memory_bundle immediately. " +
      "Relevant fact without save request → you may ask once if they want it in memory; do not auto-save everything. " +
      "Once saving: extract entities and tokens — never ask about Person entity or structure. " +
      "Retrieve: when user asks to recall or when prior knowledge helps — search_* without asking permission. " +
      "New agent: call start_here first. Never use create_memory alone for new observations.";

  public const string RefIdsGuide =
      """
      # Ref ids — agent-facing identifiers

      Every entity, memory, and token has two ids:

      | Field | Length | Use |
      |-------|--------|-----|
      | **Ref** | 8 chars (Base64url) | **Primary** — use in follow-up tool calls and reasoning |
      | **Id** | Guid | Internal / legacy — still accepted everywhere |

      ## Rules for agents

      1. **Always prefer Ref** after a store or search — saves context window.
      2. **Never ask the user** for Ref or Guid; you get them from tool responses.
      3. **Do not repeat Guid** in thinking or user-facing text when Ref is available.
      4. **Guid remains valid** — all `id` / `memoryId` / `entityId` / `tokenId` parameters accept **Ref or Guid**.

      ## After store_memory_bundle

      Response includes `memoryRef`, `entityRefs` (per clientKey), `tokenRefs`, plus Guid fields for compatibility.

      ```
      get_memory(id="aB3-xY9z")              ← Ref (preferred)
      get_entity(id="kL9mN2pQ")              ← entity Ref from find_entities / bundle
      link_memory_token(memoryId="aB3-xY9z", tokenId="pQ7rS2tU")
      search_memories_by_entity(entityId="kL9mN2pQ")
      ```

      Guid still works: `get_memory(id="3fa85f64-...")` — use only if you only have the Guid.

      ## Batch JSON

      `link_memory_tokens` and `create_and_link_tokens` accept Ref or Guid in JSON:

      ```json
      [{"memoryId":"aB3-xY9z","tokenId":"pQ7rS2tU"}]
      ```

      ## Lists and search

      `find_entities`, `search_*`, `get_memory`, etc. return **Ref first** on each item. Cache Ref for the session; re-query if unsure.
      """;

  /// <summary>Curated onboarding README — zero-parameter entry point for agents without project skills.</summary>
  public const string StartHere = """
      # START HERE — MemoryMCP agent README

      You are connected to a structured memory server. **Read this before using memory tools.**

      ## Ref ids (primary — Guid still works)

      Every entity, memory, and token has **Ref** (8-char Base64, preferred) and **Id** (Guid, backward compatible).
      - After `store_memory_bundle`: use `memoryRef`, `entityRefs`, `tokenRefs` in follow-up calls.
      - All id parameters accept **Ref or Guid** — prefer Ref to save context; never ask the user for ids.
      - Example: `get_memory(id="aB3-xY9z")` not the Guid. `search_memories_by_entity(entityId="kL9mN2pQ")`.
      - See `get_memorymcp_guide(topic="refs")` for full detail.

      ## When to SAVE (store_memory_bundle)

      ### Save immediately — no permission question
      User gives a **direct save order**, for example:
      - "Spara i minnet …"
      - "Kom ihåg det här …" / "Kom ihåg att …"
      - "Remember this …" / "Store this …"

      → Call `store_memory_bundle` right away with entities and tokens. Confirm briefly that it was saved.

      ### Ask first — optional, once
      User shares a fact in normal conversation **without** asking to save — preference, age, name, decision — and it seems worth keeping long-term.

      → You **may** ask once: "Vill du att jag ska lägga detta i minnet?"
      → If yes → `store_memory_bundle` (see structure rules below — no further questions)
      → If no or they ignore it → **do not store**

      ### Do not
      - Auto-save every casual fact without user intent
      - Ask "ska jag skapa en entitet?" or "vill du strukturera som person?" — ever
      - **Store documentation or smoke-test example text** (Maja, Sandra, böff wines, "went to Stockholm", etc.) — those JSON blocks in `get_memorymcp_guide(topic="examples")` are **illustrative only**, not user facts. Never call `store_memory_bundle` with them unless the user explicitly said that exact sentence.

      ## When to RETRIEVE (search_*)

      Search memory **without asking permission** when:
      - User asks to recall: "Vad minns du om …?", "Har jag berättat …?"
      - The task benefits from prior knowledge: preferences, people, past decisions
      - You need context about someone or something before answering

      Tools: `search_memories_by_text`, `search_memories_by_entity`, `search_memories_by_token`, `find_entities`.

      Do **not** retrieve on every message — only when recall clearly helps.

      ## Once you are saving: never ask about structure

      When the user wants something saved (explicit order or confirmed yes), **do not ask**:
      - "Vill du strukturera detta som en person (entitet)?"
      - "Ska jag skapa en entitet?"
      - "Vill du lägga till fler detaljer?"

      Infer entities and tokens from the text and call `store_memory_bundle` in one step.

      ## How to store

      ```
      store_memory_bundle(
        raw = <exact user text>,
        entitiesJson = <subjects mentioned>,
        tokensJson = <atomic facet values stated in text>,
        entityLinksJson = <which entities this memory is about>
      )
      ```

      Do **not** use `create_memory` alone — it saves raw text without entities or tokens.

      ## Example: explicit save order (nobrainer)

      **User:** "Kom ihåg: jag heter Jon Jander och jag gillar pasta"

      **You:** one `store_memory_bundle` call — no questions about entities.

      ```json
      {
        "raw": "Jag heter Jon Jander och jag gillar pasta",
        "entitiesJson": "[{\"clientKey\":\"jon\",\"type\":\"Person\",\"name\":\"Jon Jander\"}]",
        "tokensJson": "[{\"property\":\"Likes\",\"type\":\"String\",\"stringValue\":\"pasta\",\"confidence\":0.95,\"source\":\"Extracted\"}]",
        "entityLinksJson": "[\"jon\"]"
      }
      ```

      - **Entity** = who (Jon Jander, type Person). Identity lives here — no `Name` token for people.
      - **Token** = stated facet value (`Likes=pasta`). Property names are abstract and reusable.

      ## Example: fact without save order (ask first)

      **User:** "Förresten, jag gillar pasta" (no save request)

      **You:** "Vill du att jag ska lägga detta i minnet?" — only if it seems worth keeping.
      If they say yes → same bundle as above (`Likes=pasta` on Jon Jander entity if known, or ask nothing about structure).

      ## Example: person + age (after save confirmed or explicit order)

      **User:** "Spara: Jon Jander är 38 år"

      ```json
      {
        "raw": "Jon Jander är 38 år",
        "entitiesJson": "[{\"clientKey\":\"jon\",\"type\":\"Person\",\"name\":\"Jon Jander\"}]",
        "tokensJson": "[{\"property\":\"Age\",\"type\":\"Int\",\"intValue\":38,\"confidence\":0.95,\"source\":\"Extracted\"}]",
        "entityLinksJson": "[\"jon\"]"
      }
      ```

      ## Four concepts

      | Concept | Role | Example |
      |---------|------|---------|
      | Memory | Immutable raw observation | exact user text |
      | Entity | Who/what | Person "Jon Jander", Wine "böff" |
      | Token | Abstract facet + atomic value | `Likes=pasta`, `Year=1988`, `Age=38` |
      | Relationship | How entities connect | SameAgeAs, ParentOf |

      ## Token rules (short)

      1. Property names are **abstract**: `Year`, `Likes`, `Age` — never `JonBirthYear`, `LikesFood`.
      2. **One token = one value** — two favorite foods → two `Likes` tokens.
      3. **Stated in text → token.** Inferred via comparison → relationship, not copied token.
      4. Optional before store: `list_token_properties` to reuse existing vocabulary.

      ## Tool order

      1. `start_here` — you are here
      2. Decide: save now / ask to save / retrieve / neither
      3. `store_memory_bundle` or `search_*` as above
      4. `get_memorymcp_guide(topic="store"|"retrieve"|"tokens"|"examples")` — deeper detail

      ## More help

      - `get_memorymcp_guide` — topics: quickstart, store, retrieve, tokens, maintenance, examples
      - MCP resources: `memorymcp://guide/start`, `memorymcp://guide/refs`, `memorymcp://guide/workflow`, `memorymcp://guide/tokens`, `memorymcp://guide/examples`
      """;

  public static string GetGuide(string? topic = null)
  {
    var key = NormalizeTopic(topic);
    return key switch
    {
      "store" => StoreWorkflow,
      "retrieve" => RetrieveWorkflow,
      "tokens" => TokensGuide,
      "maintenance" => MaintenanceWorkflow,
      "examples" => Examples,
      "quickstart" => Quickstart,
      "start_here" => StartHere,
      "refs" or "ref" or "ids" or "identifiers" => RefIdsGuide,
      _ => Overview
    };
  }

  public static IReadOnlyList<string> AfterStoreBundleSteps =>
  [
      "Use memoryRef and entityRefs from this response in follow-up calls — prefer Ref over Guid.",
      "Verify with get_memory(memoryRef) or search_memories_by_entity / search_memories_by_token.",
      "If property names look entity-specific (e.g. SofiaBirthYear), run list_token_properties then rename_token_property (preview=true first).",
      "If a string token holds multiple values, use split_token_value with token Ref.",
      "To add facts later: create_and_link_tokens (memoryId=Ref), link_memory_tokens, or create_relationship."
  ];

  public static IReadOnlyList<string> AfterStoreBundlesSteps =>
  [
      "Results are ordered by index — match each result.index to your input array.",
      "Verify a sample with get_memory or search_memories_by_token.",
      "For many tokens on existing memories, prefer create_and_link_tokens instead of per-memory bundle calls."
  ];

  public static IReadOnlyList<string> AfterCreateMemorySteps =>
  [
      "You stored raw text only — no entities or tokens were created.",
      "For new observations prefer store_memory_bundle (see start_here). Do not ask the user; extract entities and tokens from the text.",
      "To fix this memory: create_entity, create_token, link_memory_entity, link_memory_token — or store a new bundle instead."
  ];

  private static string NormalizeTopic(string? topic)
  {
    if (string.IsNullOrWhiteSpace(topic))
      return "overview";

    return topic.Trim().ToLowerInvariant() switch
    {
      "overview" or "help" => "overview",
      "start" or "start_here" or "readme" or "onboarding" => "start_here",
      "store" or "storing" or "write" or "save" => "store",
      "retrieve" or "retrieval" or "read" or "search" or "query" => "retrieve",
      "token" or "tokens" or "tokenize" or "properties" => "tokens",
      "maintain" or "maintenance" or "cleanup" or "vocabulary" => "maintenance",
      "example" or "examples" or "sample" => "examples",
      "quick" or "quickstart" or "cheatsheet" => "quickstart",
      var other => other
    };
  }

  private const string Overview = """
      # MemoryMCP agent guide

      ## When to read this
      **New agent:** call `start_here` first. Or `get_memorymcp_guide` when you need a specific topic.
      MCP resources: `memorymcp://guide/start`, `memorymcp://guide/refs`, `memorymcp://guide/workflow`, `memorymcp://guide/tokens`, `memorymcp://guide/examples`.

      ## Core model
      - **Ref** — short 8-char id for agents (primary in tool calls); **Id** — Guid (still accepted everywhere)
      - **Memory** — immutable raw observation text
      - **Entity** — who/what (Person, Wine, Place, …)
      - **Token** — abstract facet + atomic value for mesh search (Year, Likes, Age)
      - **Relationship** — how entities connect (SameAgeAs, ParentOf, …)

      ## Workflows (topics)
      | Topic | When |
      |-------|------|
      | `start_here` | First session — when to save, retrieve, Ref ids |
      | `refs` | Ref vs Guid — which id to use in tool calls |
      | `quickstart` | Minimal cheat sheet |
      | `store` | Saving a new observation |
      | `retrieve` | Finding existing knowledge |
      | `tokens` | Token rules and token vs relationship |
      | `maintenance` | Renaming/splitting properties over time |
      | `examples` | JSON bundle examples |

      ## Golden rules
      1. **Save** on explicit order; **ask once** for relevant facts without a save request; **do not** auto-save everything.
      2. **Retrieve** when recall helps — search without asking permission.
      3. Once saving: prefer `store_memory_bundle`; never ask about entity structure.
      4. **Prefer Ref over Guid** in follow-up id parameters; Guid remains backward compatible.
      5. Token property names are abstract and reusable — never entity-prefixed.
      6. Tokens = directly stated facts; relationships = inferred or comparative links.
      7. Raw text is never modified — use `revise_memory` or `invalidate_memory` to correct.
      """;

  private const string Quickstart = """
      # MemoryMCP quickstart

      ## When to use memory
      - **Save now:** "spara i minnet", "kom ihåg det här" → `store_memory_bundle`
      - **Ask first:** relevant fact, no save order → "Vill du att jag ska lägga detta i minnet?"
      - **Retrieve:** user asks to recall, or prior knowledge helps → `search_*` (no permission needed)
      - **Neither:** casual chat with nothing worth keeping — do not touch memory tools

      ## Store (when saving)
      1. Optional: `list_token_properties`, `find_entities`
      2. `store_memory_bundle` with raw + entitiesJson + tokensJson + entityLinksJson + relationshipsJson
      3. Note `memoryRef` / `entityRefs` / `tokenRefs` in the response — use Ref in follow-ups
      4. `get_memory(ref)` or `search_memories_by_token` to verify

      ## Retrieve
      1. `search_memories_by_text` / `search_memories_by_entity` (entityId=Ref or Guid or name) / `search_memories_by_token`
      2. `get_memory` / `get_entity` / `get_token` — pass Ref from search results (Guid still works)
      3. `get_entity_graph` when relationships matter

      ## Tokens in one line
      Identity on entities; atomic facet values on tokens (`Year=1988`, not `SofiaBirthYear=1988`).

      ## Maintenance (light, ongoing)
      `list_token_properties` → `rename_token_property` / `merge_token_properties` / `split_token_value` (preview=true first)
      """;

  private const string StoreWorkflow = """
      # Store workflow

      ## When to store

      | Situation | Action |
      |-----------|--------|
      | Direct order: "spara i minnet", "kom ihåg …" | `store_memory_bundle` immediately |
      | Relevant fact, no save request | Ask once: "Vill du att jag ska lägga detta i minnet?" |
      | User says no or ignores | Do not store |
      | Casual chat, not worth keeping | Do not store |

      Once the user wants it saved, extract entities and tokens — **never** ask about Person entity or structure.

      ## Preferred: one transaction
      Use `store_memory_bundle` — do **not** split into create_memory then tokenize unless you are appending to an existing memory.

      ### Order inside the bundle call
      1. **raw** — exact observation text (immutable after save)
      2. **memoryFrom** — when the observation occurred (important for Age)
      3. **entitiesJson** — subjects mentioned (`clientKey`, `type`, `name`)
      4. **tokensJson** — abstract atomic facts (`property`, `type`, value, `confidence`, `source`)
      5. **entityLinksJson** — which entities this memory is about
      6. **relationshipsJson** — links when raw compares or relates entities (not copied facet values)
      7. **reuseTokens** — default true; false only when same value would misrepresent provenance

      ### Batch: multiple observations
      Use `store_memory_bundles` with a JSON array (max 100) — one transaction, all-or-nothing.
      To append tokens to **existing** memories: `create_and_link_tokens` (max 500) or `link_memory_tokens` for existing token ids.

      ### Before calling store
      - `list_token_properties` — discover existing vocabulary (Year vs BirthYear)
      - `find_entities` — reuse existing entities when names match

      ### After calling store
      - Response has `memoryRef`, `entityRefs`, `tokenRefs` — **use Ref** in later calls
      - `get_memory(id=memoryRef)` — confirm entities, tokens, relationships
      - `search_memories_by_token` — confirm mesh indexing works

      ## Step-by-step alternative (append or legacy)
      Only when adding to an **existing** memory or when bundle is not suitable:
      1. `create_memory` (or use existing memory Ref/Guid)
      2. `create_entity` / `find_entities` → `link_memory_entity` (entityId=Ref)
      3. `create_token` → `link_memory_token` (tokenId=Ref)
      4. `create_relationship` when entities are linked comparatively

      ## Token vs relationship at store time
      - Raw says "Sandra is 38" → token `Age=38` on Sandra
      - Raw says "Åsa is the same age as Sandra" → relationship `SameAgeAs`, **no** Age token on Åsa
      """;

  private const string RetrieveWorkflow = """
      # Retrieve workflow

      ## When to retrieve

      Search memory when recall clearly helps — **no need to ask permission**:
      - User asks: "Vad minns du om …?", "What do you remember about …?"
      - Task needs preferences, people, or past context
      - You are about to answer about someone/something that may be in memory

      Do not search on every message. Skip memory when the question is generic or self-contained.

      ## Pick a search tool
      | Goal | Tool |
      |------|------|
      | Free text in observations | `search_memories_by_text` |
      | Everything about a person/thing | `search_memories_by_entity` (entityId=**Ref** or Guid or name) |
      | Mesh: same year, likes, age, … | `search_memories_by_token` or `search_entities_by_token` |
      | Find entities by name/type | `find_entities` |
      | How entities connect | `find_relationships`, `get_entity_graph` |

      ## Drill down
      1. Search → note **Ref** on each result (Guid also present)
      2. `get_memory(id=Ref)` — full raw text, entities, tokens, relationships, revisions
      3. `get_entity` / `get_token` — pass Ref from list results
      4. `get_memory_history` / `get_token_history` — corrections and audit

      ## Inactive content
      Search excludes superseded, invalid, and retracted memories by default. Pass `includeInactive=true` only when auditing history.

      ## Light maintenance while retrieving
      If you notice messy property names during retrieval, note them and run `get_memorymcp_guide(topic="maintenance")` when done querying.
      """;

  private const string TokensGuide = """
      # Token guide

      Tokens are **abstract search facets** — a mesh layer connecting unrelated memories.

      ## Rules
      1. Property = reusable facet: `Year`, `Name`, `Likes`, `Color` — not `SofiaBirthYear`, `LikesFood`
      2. Identity on entities; values on tokens
      3. One token = one atomic value (two wines → two Name + two Year tokens)
      4. No fixed schema — discover with `list_token_properties`, converge over time

      ## Tokens vs relationships
      ```
      Stated in raw for this entity?
      ├─ YES → token (source=Extracted or Derived), entityLinks identify who
      └─ NO, but raw links entities ("same age as Sandra") → relationship, no copied token
      ```

      ## reuseTokens
      - `true` (default): two memories both state Age=38 independently → one shared token, two memory links
      - `false`: when sharing would imply a stated fact that was only inferred

      ## Entity identity
      Do not create Name tokens for people already in entityLinks. Name tokens are for facet values in text (wine name "böff").

      ## Age vs Year
      Prefer `Year` when birth/foundation year is known or stable. Use `Age` when raw gives age; set `memoryFrom` when time matters.
      """;

  private const string MaintenanceWorkflow = """
      # Maintenance workflow

      Run lightly when storing or retrieving and vocabulary looks messy.

      ## 1. Discover
      - `list_token_properties` — all property names + counts
      - `list_tokens_by_property` — popular vs rare values
      - `list_tokens_by_property(maxMemoryLinks=1)` — one-off tokens

      ## 2. Rename overly specific properties
      ```
      rename_token_property(fromProperty="SofiaBirthYear", toProperty="Year", preview=true)
      rename_token_property(fromProperty="SofiaBirthYear", toProperty="Year")
      ```

      Or merge several:
      ```
      merge_token_properties(fromPropertiesJson='["SofiaBirthYear","BirthYear"]', toProperty="Year", preview=true)
      ```

      ## 3. Split compound values
      `LikesFood="pasta, hamburgare"` → `split_token_value(tokenId="<Ref>", targetProperty="Likes")`

      ## 4. Prefer fixing at store time
      Correct abstract tokens in new `store_memory_bundle` calls rather than only cleaning up later.

      ## 5. Do not over-merge
      Merge duplicate tokens only when both memories **independently** stated the value. Inferred values belong in relationships.
      """;

  private const string Examples = """
      # Bundle examples

      **Do not store these strings as real memories.** They show JSON shape only. Copy the structure with the **user's actual words** — never re-insert Maja, Sandra, böff, Stockholm, or other placeholder names from this guide.

      ## Single subject (age + derived birth year)
      ```json
      {
        "raw": "Maja is 15 years old today. It is 2026.",
        "memoryFrom": "2026-06-23T00:00:00Z",
        "entitiesJson": "[{\"clientKey\":\"maja\",\"type\":\"Person\",\"name\":\"Maja\"}]",
        "tokensJson": "[{\"property\":\"Age\",\"type\":\"Int\",\"intValue\":15,\"confidence\":0.95,\"source\":\"Extracted\"},{\"property\":\"Year\",\"type\":\"Int\",\"intValue\":2011,\"confidence\":0.75,\"source\":\"Derived\"}]",
        "entityLinksJson": "[\"maja\"]"
      }
      ```

      ## Multiple subjects (shared property names)
      ```json
      {
        "raw": "Jag har två viner, en böff från 1988 och en foo från 2025",
        "entitiesJson": "[{\"clientKey\":\"boff\",\"type\":\"Wine\",\"name\":\"böff\"},{\"clientKey\":\"foo\",\"type\":\"Wine\",\"name\":\"foo\"}]",
        "tokensJson": "[{\"property\":\"Name\",\"type\":\"String\",\"stringValue\":\"böff\",\"confidence\":0.95,\"source\":\"Extracted\"},{\"property\":\"Name\",\"type\":\"String\",\"stringValue\":\"foo\",\"confidence\":0.95,\"source\":\"Extracted\"},{\"property\":\"Year\",\"type\":\"Int\",\"intValue\":1988,\"confidence\":0.9,\"source\":\"Extracted\"},{\"property\":\"Year\",\"type\":\"Int\",\"intValue\":2025,\"confidence\":0.9,\"source\":\"Extracted\"}]",
        "entityLinksJson": "[\"boff\",\"foo\"]"
      }
      ```

      ## Relationship instead of inferred token
      Memory A: Sandra is 38 → Age token on Sandra.
      Memory B: Åsa is the same age as Sandra → SameAgeAs relationship, no Age on Åsa.
      """;
}
