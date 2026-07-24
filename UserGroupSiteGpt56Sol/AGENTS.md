# Repository Instructions for Codex

The files in `.claude/rules/` are the canonical source of this repository's
coding conventions. Codex must use the same rules as Claude; do not duplicate
their contents here.

Before making programming decisions for a file—including planning, generating,
editing, or reviewing code—Codex must:

1. Inspect every `.claude/rules/*.instructions.md` file.
2. Read each rule file whose YAML front matter `paths` glob matches the file
   being considered.
3. Apply all matching rule files for the entire task. If additional file types
   enter the task later, load their matching rules before proceeding.

Treat the `paths` values as repository-root-relative glob patterns. A rule with
multiple patterns applies when any pattern matches. Rules without a `paths`
field apply repository-wide.

When matching rules conflict, prefer the rule with the more specific path
scope. Instructions supplied directly by the user or by the Codex runtime take
precedence over repository rules.
