// Claude Code PreToolUse hook (Bash): enforces the Git workflow from CLAUDE.md.
// Exit code 2 blocks the command and shows the reason to Claude.
import { readFileSync } from 'node:fs';

const BLOCK = 2;
const input = JSON.parse(readFileSync(0, 'utf8') || '{}');
const command = String(input?.tool_input?.command ?? '');

const GIT = String.raw`\bgit\b[^|;&]*`;
const rule = (pattern, reason) => ({ test: new RegExp(pattern, 'i'), reason });

const rules = [
  rule(`${GIT}\\s--no-verify\\b|${GIT}\\bcommit\\b[^|;&]*\\s-n(\\s|$)`, 'Do not bypass git hooks (--no-verify / commit -n). The hooks enforce linear history and commit-message rules.'),
  rule(`${GIT}\\bmerge(?=\\s|$)[^|;&]*--no-ff\\b`, 'Merge commits are not allowed. Use `git merge --squash <branch>` followed by one `git commit`.'),
  rule(`${GIT}\\bmerge(?=\\s|$)(?![^|;&]*(--squash|--ff-only|--abort|--continue|--quit))`, 'Only squash merges are allowed: `git merge --squash <branch>` then `git commit` (or `git merge --ff-only` after a rebase).'),
  rule(`${GIT}\\bpull(?=\\s|$)(?![^|;&]*(--rebase|--ff-only))`, 'Use `git pull --rebase` (or `--ff-only`) so no merge commits are created.'),
  rule(`${GIT}\\bconfig\\b[^|;&]*core\\.hooksPath(?![^|;&]*\\.githooks)`, 'Do not change core.hooksPath; it must stay `.githooks`.'),
  rule(`${GIT}(\\s-c\\s+user\\.(name|email)=|\\bconfig\\b[^|;&]*\\buser\\.(name|email)\\s+[^\\s-])`, "Do not change the git identity. Commits use the repository owner's identity."),
  rule(`(\\bgit\\b|\\bgh\\b)[\\s\\S]*(co-authored-by:|claude-session:|generated with \\[?claude|noreply@anthropic\\.com)`, 'Never add Claude as co-author or add AI attribution to commits, PRs or git identity.'),
];

const hit = rules.find((rule) => rule.test.test(command));
if (hit) {
  process.stderr.write(`Blocked by .claude/hooks/git-guard.mjs: ${hit.reason}\n`);
  process.exit(BLOCK);
}

process.exit(0);
