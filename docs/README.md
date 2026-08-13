# Yardımcı dosyalar

## `keep-alive.yml`

Render'ın ücretsiz planında servis 15 dakika hareketsizlikten sonra uyur; bu zamanlanmış
iş sağlık ucunu düzenli çağırarak uyanık tutar.

Dosya burada duruyor çünkü `.github/workflows/` altına **git push ile** konulamıyor:
GitHub, workflow dosyası içeren push'u ancak kullanılan kişisel erişim token'ında
`workflow` izni varsa kabul eder.

Devreye almak için iki yol var:

**A — GitHub arayüzünden (token'a dokunmadan)**

1. Depo → **Add file → Create new file**
2. Dosya adı: `.github/workflows/keep-alive.yml`
3. Bu klasördeki `keep-alive.yml` içeriğini yapıştırıp commit edin.

**B — Token iznini açıp taşıyarak**

github.com → Settings → Developer settings → Personal access tokens → token → `workflow`
kutusunu işaretleyin. Ardından:

```bash
mkdir -p .github/workflows && git mv docs/keep-alive.yml .github/workflows/keep-alive.yml
```

Her iki durumda da **Settings → Secrets and variables → Actions → Variables** altına
`API_URL` = `https://gameflow-api.onrender.com` değerini eklemeyi unutmayın; değişken
tanımlı değilse iş sessizce atlanır.
