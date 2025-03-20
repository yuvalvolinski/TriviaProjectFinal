## תנאים מוקדמים
- התקינו את [VS Code](https://code.visualstudio.com/download)
- התקינו את [NET Framework.](https://dotnet.microsoft.com/en-us/download)
- התקינו את [git](https://git-scm.com/downloads/win)
- התקינו TypeScript בעזרת הרצת הפקודות:
```
powershell -c "irm bun.sh/install.ps1 | iex"
bun add -g typescript
```
- תנו לתוכנה גישה לפורט הרצוי (במקרה שלנו, פורט 5000) בעזרת פתיחת שורת המשימות כמנהל והרצת הפקודה:
```
netsh http add urlacl url=http://*:5000/ user=Everyone
```

# הרצת הפרוייקט
פתחו את הפרוייקט ב-VS Code והריצו בעזרת הפקודה:
```
dotnet run
```
