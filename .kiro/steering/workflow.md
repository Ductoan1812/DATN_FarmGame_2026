# AI Agent Workflow

Codex is the orchestrator, reviewer, and tester. Kiro is the implementation agent.

Only one agent may directly edit the project at a time:
- When Codex is reviewing, testing, or editing workflow files, Kiro waits.
- When Kiro is implementing a sprint task, Codex does not edit project files.
- After Kiro finishes, Codex reviews the diff and tests in Unity.
- If tests fail, Codex writes a narrow fix task for Kiro.

Implementation rules for Kiro:
- Read only the files needed for the current task.
- Keep changes minimal and compatibility-preserving.
- Do not rewrite working systems.
- Do not rename or move files unless explicitly required.
- Do not modify scenes, prefabs, ScriptableObjects, or meta files unless the task asks for it.
- Report changed files, reason for each change, and known risks.

Token efficiency rules:
- Prefer MCP tools for Unity/project inspection, logs, component lookup, and compilation checks.
- Use shell only for short commands or when no MCP tool can do the job.
- Never dump large file contents unless the task truly needs them.
- For long search or investigation work, let Kiro search and summarize instead of pasting raw output.
- Keep responses concise and task-focused; summarize results instead of repeating command output.

Output format:
- Summary
- Changed files
- Test notes
- Risks or assumptions
