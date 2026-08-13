# ImageComparator code review skill

Use this skill to review pull requests in this repository with project-specific context.

## Focus areas

- Validate image-comparison correctness across strategies (`legacy`, `mad`, `dhash`, `auto`).
- Check for numeric overflow risks in pixel and channel accumulation logic.
- Verify `System.Drawing` pixel-format assumptions (especially 24bpp BGR byte order and stride handling).
- Review CLI argument parsing edge cases (`--strategy`, `--benchmark`, `--benchmark-iterations`).
- Ensure Windows-only behavior is explicit for `System.Drawing` paths and tests.
- Confirm benchmark and test changes remain deterministic and minimal.

## Review output expectations

- Report only high-confidence correctness, security, or reliability issues.
- Include exact file/line references and a concrete fix suggestion for each issue.
