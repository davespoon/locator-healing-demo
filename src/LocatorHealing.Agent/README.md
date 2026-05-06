# LocatorHealing.Agent

`LocatorHealing.Agent` is a .NET tool for analyzing failed Selenium UI tests, generating locator-healing candidates, and applying proposed page-object patches.

## Install

```bash
dotnet tool install --global LocatorHealing.Agent
```

## Usage

```bash
locator-healing run <test-results-directory> --repo-root <repo-root> [--output-dir <output-dir>] [--report-file <report-file>]
```

## Requirements

- A directory containing NUnit XML test result files
- Selenium failure details in the test output
- `OPENAI_API_KEY` environment variable for candidate generation

## Current assumptions

This version is tuned for the demo repository conventions, including the current page-object and test folder structure.
