#!/bin/bash
set -eu -o pipefail

# Requires the following environment variables:
#
#   INPUT_STARTING-DIR (--start-directory): Top-level directory in which to perform clean up (for example, find orphaned markdown files).
#   INPUT_DOCSET-ROOT (--docset-root): The full path to the root directory for the docset, e.g. 'c:\\users\\gewarren\\dotnet-docs\\docs'.
#   INPUT_REPO-ROOT (--repo-root): The full path to the local root directory for the repository, e.g. 'c:\\users\\gewarren\\dotnet-docs'.
#   INPUT_BASE-PATH (--base-path): The URL base path for the docset, e.g. '/windows/uwp' or '/dotnet'.
#
# Optional environment variables:
#
#   INPUT_ORPHANED-TOPICS (--orphaned-topics): Use this option to find orphaned topics.
#   INPUT_ORPHANED-IMAGES (--orphaned-images): Find orphaned .png, .gif, .jpg, or .svg files.
#   INPUT_ORPHANED-SNIPPETS (--orphaned-snippets): Find orphaned .cs and .vb files.
#   INPUT_ORPHANED-INCLUDES (--orphaned-includes): Find orphaned INCLUDE files.
#   INPUT_REPLACE-REDIRECTS (--replace-redirects): Find backlinks to redirected files and replace with new target.
#   INPUT_RELATIVE-LINKS (--relative-links): Replace site-relative links with file-relative links.
#   INPUT_REMOVE-HOPS (--remove-hops): Clean redirection JSON file by replacing targets that are themselves redirected (daisy chains).

env_var_is_set() {
	if [[ "${!1}" =~ ^[Tt]rue$|^[1]$ ]]; then
		return 0
	else
		return 1
  fi
}

# Use this option to find orphaned topics.
if env_var_is_set "INPUT_ORPHANED-TOPICS"; then
	dotnet CleanRepo.dll -- \
	--start-directory="$STARTING_DIR" \
	--docset-root="$DOCSET_ROOT" \
	--repo-root="$REPO_ROOT" \
	--base-path="$BASE_PATH" \
	--delete --orphaned-topics
fi

# Find orphaned .png, .gif, .jpg, or .svg files.
if env_var_is_set "INPUT_ORPHANED-IMAGES"; then
	dotnet CleanRepo.dll  -- \
	--start-directory="$STARTING_DIR" \
	--docset-root="$DOCSET_ROOT" \
	--repo-root="$REPO_ROOT" \
	--base-path="$BASE_PATH" \
	--delete --orphaned-images
fi

# Find orphaned .cs and .vb files.
if env_var_is_set "INPUT_ORPHANED-SNIPPETS"; then
	dotnet CleanRepo.dll  -- \
	--start-directory="$STARTING_DIR" \
	--docset-root="$DOCSET_ROOT" \
	--repo-root="$REPO_ROOT" \
	--base-path="$BASE_PATH" \
	--delete --orphaned-snippets
fi

# Find orphaned INCLUDE files.
if env_var_is_set "INPUT_ORPHANED-INCLUDES"; then
	dotnet CleanRepo.dll  -- \
	--start-directory="$STARTING_DIR" \
	--docset-root="$DOCSET_ROOT" \
	--repo-root="$REPO_ROOT" \
	--base-path="$BASE_PATH" \
	--delete --orphaned-includes
fi

# Find backlinks to redirected files and replace with new target.
if env_var_is_set "INPUT_REPLACE-REDIRECTS"; then
	dotnet CleanRepo.dll  -- \
	--start-directory="$STARTING_DIR" \
	--docset-root="$DOCSET_ROOT" \
	--repo-root="$REPO_ROOT" \
	--base-path="$BASE_PATH" \
	--replace-redirects
fi

# Replace site-relative links with file-relative links.
if env_var_is_set "INPUT_RELATIVE-LINKS"; then
	dotnet CleanRepo.dll  -- \
	--start-directory="$STARTING_DIR" \
	--docset-root="$DOCSET_ROOT" \
	--repo-root="$REPO_ROOT" \
	--base-path="$BASE_PATH" \
	--relative-links
fi

# Clean redirection JSON file by replacing targets that are themselves redirected (daisy chains).
if env_var_is_set "INPUT_REMOVE-HOPS"; then
	dotnet CleanRepo.dll  -- \
	--start-directory="$STARTING_DIR" \
	--docset-root="$DOCSET_ROOT" \
	--repo-root="$REPO_ROOT" \
	--base-path="$BASE_PATH" \
	--remove-hops
fi
