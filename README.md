# Locator Healing Demo

## Automated locator-healing pull requests

This repository includes a GitHub Actions workflow at `.github/workflows/pr-on-failed-test.yml`.

When the workflow runs on `main` or by manual dispatch, it:

1. Restores the UI tests and locator-healing projects.
2. Builds the UI tests.
3. Packs the locator-healing agent as a local .NET tool.
4. Runs the UI tests.
5. Installs and runs the locator-healing tool if the UI tests fail.
6. Creates a pull request with any proposed page-object changes for human review.
7. Uploads the NUnit results, diagnostics, and healing report as workflow artifacts.

### Required secret

Add this repository secret before expecting locator-healing PRs to be created:

- `OPENAI_API_KEY`

If the UI tests fail and the agent proposes a patch, the workflow creates a branch named like `locator-healing/<run-id>-<run-attempt>` and opens a pull request instead of changing `main` directly.

The workflow now packages the agent from `src/LocatorHealing.Agent`, installs it from a local CI tool feed, and invokes `locator-healing` as an external tool so the repository is closer to a future split between the UI tests and the agent.

If the UI tests fail but the agent cannot produce a source change, the workflow fails and still uploads the collected artifacts for investigation.
