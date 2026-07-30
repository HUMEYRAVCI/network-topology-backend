# 🌐 Network Topology Tracker - Backend API

Bu proje; kurum, bina, kat, oda, ağ cihazı (Switch, Router, AP) ve port seviyesinde hiyerarşik ağ altyapılarını izlemek, yönetmek ve topoloji haritasını sunmak amacıyla geliştirilmiş **.NET 9 Web API** tabanlı arka plan servisidir.

---

## 🛠️ Teknoloji Yığını (Tech Stack)

* **Backend:** .NET 9 Web API (C#)
* **Database:** PostgreSQL / MSSQL
* **ORM:** Entity Framework Core (Code-First)
* **Containerization:** Docker & Docker Compose
* **Authentication:** JWT (JSON Web Token) Bearer & Role-Based Authorization
* **Documentation:** Swagger UI / OpenAPI
* **Architecture:** Layered Architecture & Global Exception Middleware

---

Ağ cihazlarını, port yapılandırmalarını, bağlantı haritalarını ve sistem loglarını merkezi olarak yönetmeyi sağlayan RESTful API mimarisi.

## 🚀 Öne Çıkan Özellikler

- **Katmanlı Ağ Mimarisi:** Bina (Facility), Kat (Floor), Oda (Room) ve Cihaz (Device) hiyerarşisi.
- **Dinamik Topoloji İzleme:** Cihazlar ve portlar arası bağlantıların (Fiber, Cat6, Wireless) takibi.
- **Güvenlik & Yetkilendirme:** Cihaz erişim bilgileri (Credential) ve sistem logları için yetki bazlı veri izolasyonu.
- **ORM & Veri Bütünlüğü:** Entity Framework Core ile ilişkisel veritabanı tasarımı, döngüsel bağımlılık (Circular Reference) koruması.
- **Konteyner Desteği:** Dockerfile ile hızlı dağıtım imkanı.

## 🛠️ Kullanılan Teknolojiler

- **Platform:** .NET 8 / C#
- **ORM:** Entity Framework Core
- **Veritabanı:** PostgreSQL / MS SQL
- **Konteynerizasyon:** Docker

## 🏗️ Mimari Yapı ve Veri Modeli

Sistemdeki nesne hiyerarşisi aşağıdaki gibidir:

`Institution (Kurum) ➔ Facility (Bina/Tesis) ➔ Floor (Kat) ➔ Room (Oda) ➔ Device (Cihaz) ➔ Port ➔ Connection (Bağlantı)`

---

## 🚀 Kurulum ve Çalıştırma (Docker)

Sistemi bağımlılık olmadan tek komutla çalıştırmak için:

```bash
# Proje dizininde konteynerleri ayağa kaldırın
docker compose up -d --build
