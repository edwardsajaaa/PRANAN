# PRANAN 🔍

> **PRANAN** adalah game investigasi & eksplorasi forensik lingkungan berbasis sudut pandang orang pertama (First-Person) yang dikembangkan menggunakan **Unity 2022.3 LTS**. Pemain bertugas menelusuri kawasan hutan gambut, mengidentifikasi kejanggalan fisik dan dokumen perizinan, mengamankan barang bukti ke dalam jurnal forensik berbasis data faktual **Status Deforestasi Indonesia (STADI) 2025**, serta mengonfrontasi mandor lapangan melalui pohon keputusan narasi bercabang konvergen (*converging branching decision tree*).

---

## 🎮 6 Mekanisme Inti Game (Sesuai GDD & Use Case Diagram)

### 1. Mekanisme Navigasi & Eksplorasi Hutan 3D (First-Person Controller)
- **Kontrol Pergerakan & Kamera**: Bergerak dengan tombol **WASD**, lari santai dengan **Left Shift**, serta kontrol sudut pandang kamera dengan **Mouse**.
- **Pembatasan Kamera & Fisika Medan**: 
  - *Pitch clamping* vertikal dibatasi antara $-80^\circ$ hingga $+80^\circ$ untuk mencegah kamera terbalik.
  - Simulasi gravitasi dan deteksi tanah presisi (*ground check*).
  - Batas kemiringan lereng (*slope limit*) $45^\circ$ dan toleransi undakan medan (*step offset*) $0.3$ meter.
  - Ayunan langkah prosedural (*grounded-cinematic head bobbing*) dengan pernapasan harmonik ganda (*dual-harmonic breathing*), *lateral weight shift*, dan *landing cushion*.
  - Modifikasi friksi medan: Kecepatan pemain berkurang sebesar $35\%$ ($0.65\times$) saat menginjak kubangan lumpur bekas alat berat (`SurfaceModifier`).
- **Environmental Storytelling**: Alur medan bertransisi dari kawasan hutan alami yang rimbun menuju area bukaan lahan gundul dan kubangan alat berat tanpa ketergantungan pada teks kaku.

### 2. Mekanisme Deteksi Interaksi Spasial (Raycast / Cone-Cast & Dynamic Reticle)
- **Deteksi Bidikan Center-Dot**: Kamera utama menembakkan proyeksi raycast dan pemindaian kerucut pandang (*cone-cast* $30^\circ$) dengan verifikasi garis pandang bebas halangan (*line-of-sight occlusion*) sejauh maksimal 2.5 meter.
- **Dynamic Reticle Feedback**: Reticle di tengah layar beranimasi mulus saat menyorot objek *Interactable*, memunculkan teks prompt interaktif `[E] Periksa [Nama Objek]`.
- **Prioritas Target Berbobot**: Jika terdapat beberapa bukti berdekatan, sistem menimbang prioritas berdasarkan sudut kelurusan pandang ($60\%$) dan jarak kedekatan ($40\%$).
- **Kalkulasi Jangkauan Berbasis Permukaan**: Menghitung jarak terdekat (*closest point*) dari mata/tubuh pemain ke permukaan fisik *collider*, menjamin deteksi mulus dari segala sudut.

### 3. Mekanisme Inspeksi Barang Bukti 3D (Forensic Clue Inspector & Anomaly Hotspot)
- **Isolasi Objek (Sub-Viewport)**: Skala waktu dibekukan (`Time.timeScale = 0`), pergerakan karakter dikunci, latar belakang diburamkan dengan tangkapan *real-time bilinear blur*, dan model 3D bukti ditampilkan di depan kamera inspeksi terisolasi.
- **Manipulasi Rotasi & Zoom**: Rotasi 360 derajat menggunakan seretan mouse (*mouse drag damping*) serta *scroll wheel* untuk zoom in/out (~35% framing proporsional layar).
- **Titik Kejanggalan (Anomaly Hotspots)**: Titik observasi khusus pada barang bukti (seperti stempel izin konsesi palsu, bekas gergaji mesin rata vs tumbang alami, tanda oli alat berat) dengan validasi *gaze dwell* minimal 0.5 detik.
- **Pengamanan Bukti**: Pemain dapat mengambil barang bukti portabel (**[F]**) atau menyimpannya kembali (**[Q]**) yang langsung terintegrasi dengan jurnal investigasi.

### 4. Mekanisme Sistem Jurnal Investigasi Otomatis (State Saving / Journal System)
- **Penyimpanan Otomatis (`<<include>>`)**: Setiap tindakan inspeksi dan penemuan anomali otomatis tercatat ke dalam sistem jurnal investigasi tanpa memerlukan save point manual.
- **Manajemen Variabel Boolean STADI 2025**:
  - `hasPermitFraud` (*punya_bukti_dokumen*): Bukti stempel izin palsu/surat konsesi kadaluarsa.
  - `hasManMadeClue` (*punya_bukti_jejak*): Bukti tunggul berpola gergaji mesin rata dan jejak alat berat.
  - `hasNaturalClue`: Bukti pohon tumbang alami akibat pelapukan gambut biologis.
- **Antarmuka Edukatif**: Akses jurnal melalui tombol **[J]** menampilkan daftar bukti yang diamankan, status verifikasi kejanggalan, serta wawasan faktual laporan **Status Deforestasi Indonesia (STADI) 2025**.

### 5. Mekanisme Sistem Narasi Bercabang Konvergen (Converging Branching Dialogue)
- **Konfrontasi NPC Mandor (Pak Hendra)**: Evaluasi logika bersarang (*nested-if*) mengevaluasi kombinasi variabel boolean jurnal saat interaksi konfrontasi akhir dimulai.
- **4 Rute Skenario & 3 Variasi Ending**:
  - **Rute 1 (`hasPermitFraud = True` & `hasManMadeClue = True`)**: Opsi dialog *"Tuduh dengan Bukti Mutlak"* $\rightarrow$ **Ending A (Good Ending)**: Dalang utama ditangkap, izin konsesi dicabut, dan operasi ilegal dihentikan total.
  - **Rute 2 (`hasPermitFraud = True` & `hasManMadeClue = False`)**: Opsi dialog *"Gugat Cacat Administrasi"* $\rightarrow$ **Ending B (Neutral Ending)**: Sanksi administratif dijatuhkan dan pekerja lapangan ditahan, namun aktor intelektual lolos dari jeratan hukum pidana.
  - **Rute 3 (`hasPermitFraud = False` & `hasManMadeClue = True`)**: Kesempatan kompensasi agensi pemain:
    - Opsi dialog *"Gertak Pelaku"* $\rightarrow$ Pelaku gugup dan mengaku sebagian $\rightarrow$ **Ending B (Neutral Ending)**.
    - Opsi dialog *"Mundur / Ragu"* $\rightarrow$ Pelaku menyangkal tuduhan $\rightarrow$ **Ending C (Bad Ending)**.
  - **Rute 4 (`hasPermitFraud = False` & `hasManMadeClue = False`)**: Hanya tersedia opsi *"Interogasi Kosong"* $\rightarrow$ **Ending C (Bad Ending)**: Investigasi gagal total karena ketiadaan bukti materiil; kasus ditutup sepihak.

### 6. Mekanisme Manajemen Status & Antarmuka (Finite State Machine & UI/UX)
- **Finite State Machine (`GameStateManager`)**: Mengatur alur status permainan:
  - `Exploration`: Kontrol gerak, mouse look, dan reticle aktif.
  - `Inspection`: Kontrol gerak mati, waktu beku, manipulasi mouse diarahkan ke model 3D bukti.
  - `Journal`: Kontrol gerak mati, waktu beku, navigasi antarmuka jurnal terbuka.
  - `Dialogue`: Kontrol gerak mati, waktu beku, antarmuka pilihan konfrontasi NPC aktif.
  - `Ending`: Layar epilog interaktif dengan badge hasil investigasi dan tombol navigasi.
  - `Menu`: Status menu konfigurasi sistem.
- **Pencegahan Input Leak**: Memastikan tidak ada benturan tombol (misal karakter tidak bisa berjalan saat sedang menginspeksi barang bukti atau memilih opsi dialog).

---

## 🕹️ Tabel Kontrol & Shortcut Input

| Tombol / Input | Status Permainan | Aksi / Fungsi |
| :--- | :--- | :--- |
| **W, A, S, D** | `Exploration` | Berjalan (Maju, Kiri, Mundur, Kanan) |
| **Mouse** | `Exploration` | Mengarahkan Pandangan (Pitch clamp $-80^\circ$ s.d. $+80^\circ$) |
| **Left Shift** | `Exploration` | Lari Santai (*Sprint*) |
| **E** | `Exploration` | Periksa Bukti / Bicara dengan NPC Mandor |
| **J** | `Exploration` / `Journal` | Buka / Tutup Jurnal Investigasi Forensik |
| **Klik Kiri + Drag Mouse** | `Inspection` | Rotasi Bebas 360 Derajat Barang Bukti 3D |
| **Scroll Wheel Mouse** | `Inspection` | Zoom In / Zoom Out Kamera Studio Inspeksi |
| **F** | `Inspection` | Ambil / Amankan Bukti Fisik ke Kantong Bukti |
| **Q** / **Esc** | `Inspection` / `Journal` | Simpan & Kembali ke Mode Eksplorasi |

---

## 📁 Struktur Direktori Script

```plaintext
Assets/Script/
├── AnomalyHotspot.cs         # Titik pengamatan anomali bukti (0.5s dwell time)
├── CameraEffects.cs          # Prosedural head-bob, weight sway, landing cushion
├── ClueInspector.cs          # Studio inspeksi 3D sub-viewport & real-time blur capture
├── DialogueManager.cs        # Sistem narasi konvergen 4 rute & 3 variasi ending
├── GameStateManager.cs       # FSM pengatur state (Exploration, Inspection, Journal, Dialogue, Ending)
├── GreyboxEvidenceSetup.cs   # Setup lingkungan pengujian bukti & inisialisasi Mandor
├── InspectionUI.cs           # Tampilan HUD studio inspeksi barang bukti
├── InteractableObject.cs     # Klasifikasi bukti, dynamic reach, & smart type deduction
├── JournalManager.cs         # Sistem jurnal investigasi & laporan edukatif STADI 2025
├── MouseLook.cs              # Kontrol kamera first-person dengan pitch clamping
├── NPCMandor.cs              # Komponen interaksi NPC Mandor Lapangan (Pak Hendra)
├── PlayerInteract.cs         # Center-dot raycasting & weighted target scanner
├── PlayerMovement.cs         # Kontroler fisika gerak, kemiringan lereng, & mud drag
├── ReticleUI.cs              # Tampilan reticle dinamis & prompt interaktif
└── SurfaceModifier.cs        # Pengubah friksi permukaan tanah/lumpur
```

---

## 🛠️ Build & Verifikasi

Proyek ini terverifikasi bersih tanpa error kompilasi menggunakan .NET SDK Unity:

```bash
dotnet build Assembly-CSharp.csproj
```
