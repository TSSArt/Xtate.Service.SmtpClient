SET GIT_MERGE_AUTOEDIT=no

git subtree pull --prefix src/Xtate.SharedContent            https://github.com/TSSArt/Xtate.SharedContent            main --squash
git subtree pull --prefix src/Xtate.IoC                      https://github.com/TSSArt/Xtate.IoC                      main --squash
git subtree pull --prefix src/Xtate.Core                     https://github.com/TSSArt/Xtate.Core                     main --squash
git subtree pull --prefix src/Xtate.DataModel.EcmaScript     https://github.com/TSSArt/Xtate.DataModel.EcmaScript     main --squash
git subtree pull --prefix src/Xtate.Logger.Serilog           https://github.com/TSSArt/Xtate.Logger.Serilog           main --squash
git subtree pull --prefix src/Xtate.IoProcessor.Http         https://github.com/TSSArt/Xtate.IoProcessor.Http         main --squash
git subtree pull --prefix src/Xtate.IoProcessor.Http.Kestrel https://github.com/TSSArt/Xtate.IoProcessor.Http.Kestrel main --squash
