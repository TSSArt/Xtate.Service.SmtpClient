@echo off
SET GIT_MERGE_AUTOEDIT=no

for /f "delims=" %%r in (subtree-repos.txt) do (
    echo.
    echo Pulling repository: %%r
    echo git subtree pull --prefix src/%%r https://github.com/TSSArt/%%r main --squash
    git subtree pull --prefix src/%%r https://github.com/TSSArt/%%r main --squash
)