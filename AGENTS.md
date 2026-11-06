## Bug Prevention and Regression Prevention

- Never allow a previously reported and resolved issue to happen again.
- Before implementing any changes, review previously reported bugs, their root causes, and their fixes.
- Treat previously reported issues as permanent regression-prevention requirements.
- When modifying code, preserve existing working functionality and avoid introducing regressions.
- Check related scripts, components, references, dependencies, and execution order before making changes.
- After implementing a fix, verify that the original issue is resolved and that related functionality still works.
- Do not reintroduce previously removed code, incorrect logic, or known problematic implementations.
- If a previous fix is incomplete or could cause the same issue again, address the underlying root cause rather than applying another temporary workaround.
- If verification is not possible, explicitly state what remains unverified. Never claim that an issue is fixed without sufficient verification.
