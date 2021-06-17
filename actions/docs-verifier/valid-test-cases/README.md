# Test cases

This folder holds actual test cases that the tool will run *directly* against through two GitHub Actions (latest version and `main` branch). This is very similar to bootstrapping.

When a new test case is added and fixed, it's very likely that latest version workflow will fail, but `main` branch workflow run is expected to succeed. Currently, ignore the failing one. Consider retiring the latest version workflow in the future.
