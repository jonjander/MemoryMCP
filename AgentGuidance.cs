namespace MemoryMCP;

/// <summary>
/// Agent workflow and usage guidance exposed via server instructions, MCP resources, and the guide tool.
/// </summary>
public static class AgentGuidance
{
  public const string ServerInstructions =
      "MemoryMCP stores long-term observations as immutable raw memories with entities, abstract tokens, and relationships. " +
      "Call get_memorymcp_guide first when unsure. Preferred store path: store_memory_bundle or store_memory_bundles (batch). " +
      "Before storing, optionally list_token_properties and find_entities. After storing, use search_* tools to verify. " +
      "Read memorymcp://guide/workflow and memorymcp://guide/tokens for full workflows.";

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
      _ => Overview
    };
  }

  public static IReadOnlyList<string> AfterStoreBundleSteps =>
  [
      "Verify with get_memory or search_memories_by_entity / search_memories_by_token.",
      "If property names look entity-specific (e.g. SofiaBirthYear), run list_token_properties then rename_token_property (preview=true first).",
      "If a string token holds multiple values, use split_token_value.",
      "To add facts to an existing memory later: create_and_link_tokens, link_memory_tokens, or create_relationship."
  ];

  public static IReadOnlyList<string> AfterStoreBundlesSteps =>
  [
      "Results are ordered by index — match each result.index to your input array.",
      "Verify a sample with get_memory or search_memories_by_token.",
      "For many tokens on existing memories, prefer create_and_link_tokens instead of per-memory bundle calls."
  ];

  public static IReadOnlyList<string> AfterCreateMemorySteps =>
  [
      "Prefer store_memory_bundle for new observations — it stores raw, entities, tokens, and relationships atomically.",
      "If you only stored raw text: create_entity or find_entities, then create_token with abstract properties, then link_memory_entity and link_memory_token.",
      "Call get_memorymcp_guide(topic=\"store\") for the full step-by-step flow."
  ];

  private static string NormalizeTopic(string? topic)
  {
    if (string.IsNullOrWhiteSpace(topic))
      return "overview";

    return topic.Trim().ToLowerInvariant() switch
    {
      "overview" or "help" or "start" => "overview",
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
      Call `get_memorymcp_guide` when you are new to this server or unsure about tool order.
      MCP resources: `memorymcp://guide/workflow`, `memorymcp://guide/tokens`, `memorymcp://guide/examples`.

      ## Core model
      - **Memory** — immutable raw observation text
      - **Entity** — who/what (Person, Wine, Place, …)
      - **Token** — abstract facet + atomic value for mesh search (Year, Likes, Age)
      - **Relationship** — how entities connect (SameAgeAs, ParentOf, …)

      ## Workflows (topics)
      | Topic | When |
      |-------|------|
      | `quickstart` | Minimal cheat sheet |
      | `store` | Saving a new observation |
      | `retrieve` | Finding existing knowledge |
      | `tokens` | Token rules and token vs relationship |
      | `maintenance` | Renaming/splitting properties over time |
      | `examples` | JSON bundle examples |

      ## Golden rules
      1. Prefer `store_memory_bundle` over separate create/link calls.
      2. Token property names are abstract and reusable — never entity-prefixed.
      3. Tokens = directly stated facts; relationships = inferred or comparative links.
      4. `list_token_properties` before storing when vocabulary might already exist.
      5. Raw text is never modified — use `revise_memory` or `invalidate_memory` to correct.
      """;

  private const string Quickstart = """
      # MemoryMCP quickstart

      ## Store (preferred)
      1. Optional: `list_token_properties`, `find_entities`
      2. `store_memory_bundle` with raw + entitiesJson + tokensJson + entityLinksJson + relationshipsJson
      3. `get_memory` or `search_memories_by_token` to verify

      ## Retrieve
      1. `search_memories_by_text` / `search_memories_by_entity` / `search_memories_by_token`
      2. `get_memory` for full detail and revision history
      3. `get_entity_graph` when relationships matter

      ## Tokens in one line
      Identity on entities; atomic facet values on tokens (`Year=1988`, not `SofiaBirthYear=1988`).

      ## Maintenance (light, ongoing)
      `list_token_properties` → `rename_token_property` / `merge_token_properties` / `split_token_value` (preview=true first)
      """;

  private const string StoreWorkflow = """
      # Store workflow

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
      - `get_memory` — confirm entities, tokens, relationships
      - `search_memories_by_token` — confirm mesh indexing works

      ## Step-by-step alternative (append or legacy)
      Only when adding to an **existing** memory or when bundle is not suitable:
      1. `create_memory` (or use existing memory id)
      2. `create_entity` / `find_entities` → `link_memory_entity`
      3. `create_token` → `link_memory_token`
      4. `create_relationship` when entities are linked comparatively

      ## Token vs relationship at store time
      - Raw says "Sandra is 38" → token `Age=38` on Sandra
      - Raw says "Åsa is the same age as Sandra" → relationship `SameAgeAs`, **no** Age token on Åsa
      """;

  private const string RetrieveWorkflow = """
      # Retrieve workflow

      ## Pick a search tool
      | Goal | Tool |
      |------|------|
      | Free text in observations | `search_memories_by_text` |
      | Everything about a person/thing | `search_memories_by_entity` (id or name) |
      | Mesh: same year, likes, age, … | `search_memories_by_token` or `search_entities_by_token` |
      | Find entities by name/type | `find_entities` |
      | How entities connect | `find_relationships`, `get_entity_graph` |

      ## Drill down
      1. Search → memory ids
      2. `get_memory` — full raw text, entities, tokens, relationships, revisions
      3. `get_entity` / `get_token` — detail and linked memories
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
      `LikesFood="pasta, hamburgare"` → `split_token_value(tokenId="...", targetProperty="Likes")`

      ## 4. Prefer fixing at store time
      Correct abstract tokens in new `store_memory_bundle` calls rather than only cleaning up later.

      ## 5. Do not over-merge
      Merge duplicate tokens only when both memories **independently** stated the value. Inferred values belong in relationships.
      """;

  private const string Examples = """
      # Bundle examples

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
