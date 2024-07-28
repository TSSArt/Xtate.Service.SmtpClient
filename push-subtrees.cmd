@echo off
SET GIT_MERGE_AUTOEDIT=no

for /f "delims=" %%r in (subtree-repos.txt) do (
    rem echo.
    rem echo Pushing repository: %%r
    rem echo git subtree push --prefix src/%%r https://github.com/TSSArt/%%r main
    start "%%r - Pushing" cmd /K git subtree push --prefix src/%%r https://github.com/TSSArt/%%r main
)
