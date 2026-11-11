# YMH219 - Chat Uygulaması

Nesne Tabanlı Programlama (YMH 219) dersi için Windows Forms ile geliştirilen istemci-sunucu sohbet uygulaması. Sunucu `TcpListener` ile bağlantıları kabul eder, istemciler `TcpClient` ile bağlanır.

> Proje geliştirme aşamasındadır. Şu an sunucu ve istemci pencerelerinin arayüz iskeleti hazırdır.

## Proje Yapısı

- `ChatApp.Core` - ağ ve mesaj mantığı (arayüzden bağımsız sınıf kütüphanesi)
- `ChatApp.Server` - sunucu uygulaması (Windows Forms)
- `ChatApp.Client` - istemci uygulaması (Windows Forms)

## Kullanılan Teknolojiler

- C# / .NET 9
- Windows Forms

## Çalıştırma Adımları

1. [.NET 9 SDK](https://dotnet.microsoft.com/download) kurulu olmalıdır (Windows).
2. Depoyu klonlayın:
   ```bash
   git clone https://github.com/kubradmrgc/YMH219-ChatApp.git
   cd YMH219-ChatApp
   ```
3. Sunucuyu ve istemciyi ayrı terminallerde çalıştırın:
   ```bash
   dotnet run --project ChatApp.Server
   dotnet run --project ChatApp.Client
   ```

## Ekran Görüntüleri

<!-- Ekran görüntüleri daha sonra docs/screenshots klasörüne eklenecek. -->

## Öğrenci

- Ad Soyad: Kübra Demirgüç
- Öğrenci No: 230541115
