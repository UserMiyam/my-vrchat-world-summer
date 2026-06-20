

```
* [new branch]      main -> main
```

これは「ローカルの `main` ブランチが、GitHub上の `my-vrchat-world` リポジトリに正常にアップロードされた」という意味です。

GitHubのリポジトリページ（`https://github.com/UserMiyam/my-vrchat-world`）を開くと、`my_vrchat_world_1` フォルダの中にUnityプロジェクトのファイル一式が表示されているはずです。

---

**現在の状態まとめ**

```
my-vrchat-world/          ← GitHubと連携済みのリポジトリ
├── .gitignore             ← 作成済み（Library等を除外）
└── my_vrchat_world_1/      ← Unityプロジェクト本体
```

これで、ALCOM・Unity・GitHubの連携がすべて完了しました。

---

**今後の運用（作業後の流れ）**

1. Unityで作業 → 保存 → Unity終了
2. PowerShellで以下を実行

```powershell
cd C:\Users\021uc\AppData\Local\VRChatProjects\my-vrchat-world
git add .
git commit -m "変更内容のメモ"
git push origin main
```

3. 壊れたら直前のコミットに戻す

```powershell
git reset --hard HEAD
```

特定の過去のコミットに戻したい場合は `git log --oneline` で確認してから `git reset --hard <コミットID>` です。

---

GitHub上（リモート）でREADMEなどを追加・編集した場合、それをローカルに反映するには **`git pull`** を使います。

```powershell
cd C:\Users\021uc\AppData\Local\VRChatProjects\my-vrchat-world
git pull origin main
```

これで、リモート（GitHub）にあってローカルにない変更（README.mdなど）が、ローカルのフォルダにダウンロードされます。

---

**注意点**

もし**ローカル側でも何か変更してまだコミットしていない**状態で `git pull` をすると、競合（コンフリクト）が起きることがあります。安全に進めるなら、先に状態を確認してください。

```powershell
git status
```

- 「`nothing to commit, working tree clean`」と表示されればOK → そのまま `git pull origin main` でOK
- 何か変更が表示される場合 → 先に `git add .` → `git commit -m "..."` してから `git pull origin main`

---
