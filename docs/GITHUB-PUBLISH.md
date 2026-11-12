# 发布源码与程序包 / Publish source and binaries

## 中文：第一次发布

本页只教你操作，不会创建仓库、修改 Git 或上传。示例账号 `YOUR_GITHUB_NAME`、仓库名 `YOUR_REPOSITORY` 都是占位符，必须替换成你自己的真实值。请用 Windows PowerShell；打包脚本另需 PowerShell 7.2+。

### 1. 建立独立新仓库

登录 GitHub，打开 [New repository](https://github.com/new)，选择自己的账号、仓库名及 Public。不要勾选自动添加 README、.gitignore 或许可证：当前项目已经包含这些文件。点击 Create repository 建立空仓库。项目首页保留一句原项目来源，LICENSE、NOTICE 和代码内原有版权声明保留。官方参考：[新建仓库](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-new-repository)。

本地已有完整开发目录，不要用新克隆覆盖它。在该目录打开 PowerShell，先只检查（将下面的 `C:\path\to\ROG-KB-RGB-Studio` 换成你的实际项目目录）：

```powershell
Set-Location 'C:\path\to\ROG-KB-RGB-Studio'
git status --short
git branch --show-current
git remote -v
git log -1 --oneline
```

当前开发基线来自 `a838070`，大量新增文件尚未进入旧提交。发布必须提交完整改动，不能只给旧 HEAD 打标签。后续示例以 `main` 为发布分支；若上面的实际分支不同，先确认希望发布的分支，再替换示例分支名，不要盲目改名或重置。

### 2. 配置自己的远端

仅当检查确认 `origin` 仍是 `https://github.com/bbfox0703/RSS_II_RGB.git` 且尚无 `upstream`，执行：

```powershell
git remote rename origin upstream
git remote add origin https://github.com/YOUR_GITHUB_NAME/YOUR_REPOSITORY.git
git remote -v
```

独立 GitHub 仓库不需要清空本地提交历史；不要删除 .git 或重新初始化。先替换 URL 的两个占位符。结果应是 `upstream` 指向原作者、`origin` 指向你的独立仓库。若已经配置正确就跳过，不要重复执行。官方参考：[管理远端](https://docs.github.com/en/get-started/git-basics/managing-remote-repositories)。

### 3. 检查、提交和推送源码

```powershell
git status --short
git diff --stat
git diff
```

`git diff` 不展示未跟踪文件的内容；用编辑器检查 `git status` 中 `??` 的新增文件。确认没有私人配置、动画缓存、日志、密钥和发行二进制，也确认所有新增源码、图标、双语文档、脚本及工作流都在提交范围内。`dist`、`bin`、`obj` 应保持排除。不要提交下载的第三方作品来凑测试素材。

检查后执行：

```powershell
git add .
git diff --cached --stat
git diff --cached --check
git diff --cached
git status --short
```

暂存检查确认无误后再提交与推送：

```powershell
git commit -m "Prepare 75HE Full and Lite preview release"
git push -u origin main
```

GitHub 可能打开浏览器要求登录。若提交要求身份，按 Git 提示设置你自己的姓名和邮箱（可用 GitHub 提供的隐私邮箱）。若 push 被拒绝，不要 force push：先检查分支和远端；若新仓库已被初始化或有人推送了提交，需要读取远端并处理差异。不要删除工作目录或用 reset 丢弃开发内容。

### 4. 选一种发布路径

**推荐标签触发 CI：** 在自己的新仓库的 Actions 页面确认工作流已启用；检查测试通过。推送普通分支不会发布附件。准备好后执行：

```powershell
git status --short
git tag -a v0.1.0-preview.1 -m "75HE Full and Lite preview"
git push origin v0.1.0-preview.1
```

打标签前工作目录应干净且对应源码已推送。`v*` 标签会触发准备好的 Release 工作流，测试成功后构建并创建 Pre-release，上传 Full、Lite、Source、SHA 四份附件。查看 Actions 的执行结果，失败时读取对应步骤，不要另开一个同名手动 Release 掩盖失败。不要给同一版本反复移动标签；修复后使用新的预览版本号。

**手动 Release：** 若要发布本机已验证的包，先在仓库 Actions 设置中临时关闭 Release 工作流，避免创建 `v*` 标签时也自动发布；或先等待该工作流处理并只使用自动生成的 Release。不要同时走两条路径。手动路径在源码提交后运行 `pwsh -File tools/Publish-Studio.ps1`（已有输出会被拒绝，请另选新输出目录；固定名称的归档也需先显式保留到别处），完成实体及干净系统验证，再在 GitHub Releases → Draft a new release 选择**刚推送源码对应的提交**创建 `v0.1.0-preview.1`，标记 Pre-release，写说明并上传下列四份文件：

- `ROG-KB-RGB-Studio-win-x64-full.zip`
- `ROG-KB-RGB-Studio-win-x64-lite.zip`
- `ROG-KB-RGB-Studio-source.zip`
- `SHA256SUMS.txt`

发布前检查附件名、哈希、源码与实际构建一致。GitHub 自带的 Source code (zip/tar.gz) 是标签提交快照，不能包含你的未提交修改；仍请附上脚本产生的对应源码包。官方参考：[管理 Release](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository)、[关于源码归档](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases)。

### 5. 发布前确认

保留 LICENSE、NOTICE 和第三方许可；确认源码和自有图形全部提交，品牌设备名称仅用于兼容性说明。验证 Full 新 GIF 导入、Lite 导入禁用且已有动画播放、两版音频/按键/灯条/自定义、正常退出恢复，以及没有安装 .NET 的 Windows x64 环境。记录测试所用键盘固件和系统，不能把已有本机测试写成全型号保证。发行包未代码签名。

## English: first release

This guide does not create repositories, change Git or upload anything. Replace `YOUR_GITHUB_NAME` and `YOUR_REPOSITORY` in commands with your own values. Use Windows PowerShell; packaging requires PowerShell 7.2+.

1. **Create an independent empty repository** at [New repository](https://github.com/new), choose your account, repository name and Public visibility. Do not initialize a README, .gitignore or licence: they already exist locally. Keep a short upstream attribution in the README and preserve LICENSE, NOTICE and existing copyright notices. Keep your existing checkout; do not overwrite it with a clone. See GitHub's [new repository guide](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-new-repository).
2. Run the inspection commands above. The old local baseline is `a838070`; new and modified files must be committed before tagging. Commands assume `main`: check your actual branch and substitute the intended release branch if different.
3. Only if `origin` still points at upstream and no `upstream` remote exists, rename `origin` to `upstream`, then add your new repository as `origin` using the commands above. Verify the URLs; skip if already correct. See [remote management](https://docs.github.com/en/get-started/git-basics/managing-remote-repositories).
4. Inspect `git status`, `git diff`, and untracked files in your editor. Exclude private settings, cached animations, logs, credentials and generated binaries. Include every intended source file, icon, document, script and workflow. Run `git add .`, inspect the staged diff and its whitespace check, then commit and push as shown above. Browser sign-in may be needed. If Git requests identity, provide your own name/email. If push is rejected, inspect remote/branch divergence instead of forcing or resetting away changes.
5. **Choose one release path.** Recommended: enable Actions in your new repository, ensure source is committed/pushed and the tree is clean, then push annotated tag `v0.1.0-preview.1` with the commands above. The prepared `v*` workflow tests, builds and creates a prerelease with Full, Lite, Source and SHA assets. A branch push alone does not publish them. Inspect Actions failures and use a new version tag after fixes instead of moving published tags.
6. For a **manual release**, temporarily disable the Release workflow before creating a matching tag, so CI does not also publish the same version. Build with `pwsh -File tools/Publish-Studio.ps1` after committing source. Existing output/archives are deliberately rejected; preserve them elsewhere or choose new output as appropriate. Test the exact packages, then use Releases → Draft a new release, target the committed/pushed source, mark Pre-release and attach the four filenames listed above. Do not run manual and automated publication for the same tag simultaneously. See [release management](https://docs.github.com/en/repositories/releasing-projects-on-github/managing-releases-in-a-repository).
7. Verify checksums and corresponding source. GitHub's automatic source archives represent the tagged commit, excluding local uncommitted files; also attach the publisher's source archive. See [about releases](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases).

Before publication, preserve project and third-party notices, commit the custom artwork, verify Full GIF import and Lite's disabled import with retained animation playback, and test both variants' audio, key/strip/custom effects and normal restoration. Test on Windows x64 without .NET installed. State actual keyboard firmware and environment coverage; the unsigned preview is not a universal compatibility guarantee.
