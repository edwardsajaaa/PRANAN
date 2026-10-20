# PRANAN 🔍

> **PRANAN** adalah game investigasi & eksplorasi forensik berbasis sudut pandang orang pertama (First-Person) yang dikembangkan menggunakan **Unity 2022.3 LTS**. Pemain bertugas menelusuri lingkungan, menemukan barang bukti fisik, dan menganalisis anomali forensik secara mendalam melalui mode inspeksi interaktif 3D.

---

## 🎮 Fitur Utama

### 1. Eksplorasi First-Person Responsif
- **Sistem Gerak Adaptif**: Berjalan, berlari (*sprint*), berjongkok (*crouch*), dan gravitasi presisi via `CharacterController`.
- **Dinamika Kamera & Imersi**: Efek *head bobbing*, *camera sway*, dan pernapasan halus (*breathing effect*) yang menyesuaikan kecepatan gerakan.
- **Deteksi Permukaan & Langkah Kaki**: Modifikasi kecepatan serta audio berdasarkan jenis permukaan/medan.

### 2. Sistem Deteksi & Interaksi Forensik Presisi (Center-Dot)
- **Center-Dot Precision Targeting**: Interaksi wajib diarahkan tepat pada titik tengah crosshair (*reticle dot*), mencegah trigger yang tidak disengaja saat pandangan meleset.
- **Collider-Based Dynamic Reach**: Jangkauan interaksi dihitung secara otomatis dan adaptif berdasarkan dimensi fisik *collider* objek (objek kecil, sedang, hingga besar).
- **Surface Proximity Calculation**: Menggunakan kalkulasi *closest point* dari posisi mata dan tubuh pemain terhadap permukaan *collider*, memungkinkan deteksi mulus dari sudut manapun (360°) asalkan titik tengah diarahkan ke objek.
- **Reticle HUD Interaktif**: Crosshair dinamis yang memunculkan prompt `[E] Periksa` tepat di bawah titik tengah saat menyorot barang bukti dalam jangkauan.

### 3. Studio Inspeksi Barang Bukti (Clue Isolator)
- **Sub-Viewport Studio**: Lingkungan studio terisolasi dengan pencahayaan *Three-Point Lighting* (Key, Fill, Back Light) yang menyorot detail fisik bukti.
- **Real-Time Blurred Background**: Mengambil *frame* gameplay saat interaksi dimulai dan merendernya dengan efek *bilinear blur*, menjaga atmosfer latar belakang tetap hidup namun fokus tetap pada objek.
- **Rotasi Giroskopik & Damping**: Rotasi bebas objek 3D menggunakan drag mouse dengan peluruhan inersia (*inertial damping*).
- **Framing Kamera Adaptif**: Kamera studio secara otomatis mengukur *bounds* objek sehingga objek tampil proporsional (~35% tinggi layar) saat awal dibuka, dilengkapi fitur zoom in/out yang fleksibel.
- **Deteksi Anomali (Hotspots)**: Titik anomali pada barang bukti dengan sistem *gaze dwell progress* untuk mengungkap petunjuk tersembunyi.

---

## 🕹️ Kontrol Dasar

| Tombol / Input | Aksi |
| :--- | :--- |
| **W, A, S, D** | Bergerak (Maju, Kiri, Mundur, Kanan) |
| **Mouse** | Menoleh / Mengarahkan Pandangan |
| **Left Shift** | Berlari (*Sprint*) |
| **Left Ctrl / C** | Berjongkok (*Crouch*) |
| **E** | Berinteraksi / Memeriksa Bukti (*Inspect*) |
| **Klik Kiri + Geser Mouse** | Memutar Barang Bukti (*Mode Inspeksi*) |
| **Scroll Mouse** | Zoom In / Zoom Out Bukti (*Mode Inspeksi*) |
| **ESC** | Keluar dari Mode Inspeksi / Menu |

---

## 📁 Struktur Direktori Utama

```plaintext
PRANAN/
├── Assets/
│   ├── Scenes/
│   │   ├── MainMenu.unity            # Menu utama
│   │   └── Gameplay.unity            # Scene gameplay utama
│   ├── Script/
│   │   ├── AnomalyHotspot.cs         # Titik pengamatan anomali bukti
│   │   ├── CameraEffects.cs          # Efek kamera, sway, dan head bobbing
│   │   ├── ClueInspector.cs          # Studio inspeksi 3D, blur background, rotasi & zoom
│   │   ├── GameStateManager.cs       # State manager (FreeRoam, Inspection, Pause)
│   │   ├── GreyboxEvidenceSetup.cs   # Setup bukti uji coba / greyboxing
│   │   ├── InspectionUI.cs           # Tampilan HUD & info bukti pada mode inspeksi
│   │   ├── InteractableObject.cs     # Komponen dasar bukti & kalkulasi jangkauan collider
│   │   ├── MouseLook.cs              # Kontrol rotasi kamera first-person
│   │   ├── PlayerInteract.cs         # Raycasting & spherecast interaksi pemain
│   │   ├── PlayerMovement.cs         # Controller pergerakan karakter utama
│   │   ├── ReticleUI.cs              # Reticle / crosshair interaktif
│   │   └── SurfaceModifier.cs        # Pengubah properti permukaan fisik
│   ├── TerrainSampleAssets/          # Aset tekstur dan medan lingkungan
│   └── TextMesh Pro/                 # Konfigurasi font & rendering UI
├── Packages/                         # Konfigurasi dependensi Unity
└── ProjectSettings/                  # Pengaturan proyek Unity
```

---

## 🛠️ Persyaratan Sistem & Instalasi

### Prasyarat
- **Unity Editor**: `2022.3.62f3 LTS` (atau versi Unity 2022.3 LTS yang kompatibel).
- **Git & Git LFS**: Wajib mengaktifkan [Git Large File Storage (Git LFS)](https://git-lfs.com/) sebelum melakukan clone aset besar (terrain, mesh, textures).

### Cara Menjalankan Proyek
1. **Clone repositori**:
   ```bash
   git clone https://github.com/edwardsajaaa/PRANAN.git
   cd PRANAN
   ```
2. **Pastikan Git LFS terpasang**:
   ```bash
   git lfs install
   git lfs pull
   ```
3. **Buka dengan Unity Hub**:
   - Pilih *Add project from disk* dan arahkan ke folder `PRANAN`.
   - Pastikan menggunakan Unity Editor versi **2022.3.62f3**.
4. **Buka Scene**:
   - Arahkan ke `Assets/Scenes/Gameplay.unity` (atau `MainMenu.unity`), lalu tekan tombol **Play** di Unity Editor.
