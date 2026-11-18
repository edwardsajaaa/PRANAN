# PRANAN

PRANAN adalah game investigasi dan eksplorasi forensik lingkungan berbasis sudut pandang orang pertama (First-Person) yang dikembangkan menggunakan engine Unity 2022.3 LTS. Pemain bertugas menelusuri kawasan Hutan Lindung Rano Towa, mengidentifikasi kejanggalan fisik dan dokumen perizinan, mengamankan barang bukti ke dalam jurnal forensik berbasis data faktual Status Deforestasi Indonesia (STADI) 2025, serta mengonfrontasi mandor lapangan melalui pohon keputusan narasi bercabang konvergen (*converging branching decision tree*).

---

## Mekanisme Inti Permainan

### 1. Sistem Prolog Sinematik dan Narasi Awal (Prologue Controller)
- **Layar Hitam Awal Berdurasi (Initial Black Screen Delay)**: Saat permainan dimulai, panel hitam penuh muncul terlebih dahulu selama durasi tertentu sebelum teks judul dan narasi diaktifkan.
- **Efek Ketik Transisional (Typewriter Effect)**: Teks prolog disajikan menggunakan animasi ketik per karakter berbasis `maxVisibleCharacters` TextMeshPro untuk menjaga stabilitas tata letak tanpa gangguan *word wrapping*.
- **Akselerasi Input (Click-to-Complete)**: Pemain dapat mengklik mouse atau menekan tombol untuk langsung menyelesaikan pengetikan kalimat, lalu klik berikutnya digunakan untuk berpindah halaman.
- **Paginasi Narasi Multihalaman**: Mendukung navigasi cerita bertahap melalui deteksi otomatis objek narasi terpisah pada Hierarchy maupun satu komponen teks terpadu.
- **Penguncian Status Permainan**: State otomatis diatur ke `GameState.Menu` selama prolog aktif untuk mencegah pergerakan karakter, lalu bertransisi mulus (*fade out*) ke `GameState.Exploration`.

### 2. Mekanisme Navigasi dan Eksplorasi Medan Hutan (First-Person Controller)
- **Kontrol Pergerakan dan Kamera**: Bergerak menggunakan tombol WASD, berlari dengan Left Shift, serta kontrol rotasi sudut pandang menggunakan Mouse.
- **Pembatasan Kamera dan Fisika Medan**:
  - Pembatasan pitch vertikal dibatasi antara -80 derajat hingga +80 derajat untuk stabilitas orientasi.
  - Simulasi gravitasi dan verifikasi kontak tanah (*ground check*) presisi.
  - Batas kemiringan lereng (*slope limit*) 45 derajat dan toleransi undakan medan (*step offset*) 0.3 meter.
  - Simulasi ayunan langkah prosedural (*head bobbing*) dengan pernapasan harmonik ganda, pergeseran bobot lateral, serta bantalan pendaratan (*landing cushion*).
  - Modifikasi friksi medan: Kecepatan pemain berkurang sebesar 35 persen saat melintasi area kubangan lumpur alat berat (`SurfaceModifier`).

### 3. Deteksi Interaksi Spasial (Raycast / Cone-Cast dan Dynamic Reticle)
- **Deteksi Bidikan Titik Tengah**: Kamera utama memproyeksikan raycast dan pemindaian kerucut pandang (*cone-cast* 30 derajat) dengan verifikasi garis pandang bebas halangan sejauh maksimal 2.5 meter.
- **Umpan Balik Reticle Dinamis**: Reticle di tengah layar bertransisi saat menyorot objek interaktif, memunculkan prompt kontekstual interaksi.
- **Pembobotan Prioritas Sasaran**: Sistem menimbang prioritas target berdasarkan kelurusan sudut pandang (bobot 60 persen) dan kedekatan jarak fisik (bobot 40 persen).
- **Kalkulasi Jarak Berbasis Permukaan**: Mengukur jarak terdekat dari posisi pemain ke permukaan *collider* fisik target.

### 4. Inspeksi Bukti Tiga Dimensi (Forensic Clue Inspector dan Anomaly Hotspot)
- **Isolasi Objek Sub-Viewport**: Waktu permainan dibekukan (`Time.timeScale = 0`), kontrol karakter dinonaktifkan, latar belakang diburamkan dengan tangkapan *real-time bilinear blur*, dan model barang bukti ditampilkan di studio inspeksi terisolasi.
- **Manipulasi Rotasi dan Perbesaran**: Rotasi 360 derajat menggunakan seretan mouse (*mouse drag damping*) serta pembesaran/pengecilan menggunakan roda mouse (*scroll wheel*).
- **Titik Pengamatan Anomali (Anomaly Hotspots)**: Titik observasi spesifik pada barang bukti (seperti stempel izin konsesi fiktif, pola potongan mesin versus tumbang alami, jejak pelumas alat berat) dengan validasi durasi tatap (*gaze dwell*) minimal 0.5 detik.
- **Penyimpanan Bukti Portabel**: Pemain dapat mengamankan barang bukti ke dalam inventaris investigasi atau menyimpannya kembali ke posisi semula.

### 5. Sistem Jurnal Investigasi Forensik (STADI 2025 State Tracking)
- **Pencatatan Otomatis**: Setiap proses inspeksi dan penemuan anomali langsung tersimpan ke sistem jurnal tanpa memerlukan titik simpan manual.
- **Variabel Pelacak Bukti**:
  - `hasPermitFraud`: Indikator bukti dokumen izin konsesi fiktif atau kedaluwarsa.
  - `hasManMadeClue`: Indikator bukti fisik aktivitas tebangan mesin liar dan jejak alat berat.
  - `hasNaturalClue`: Indikator bukti pohon tumbang alami akibat degradasi biologis tanah gambut.
- **Antarmuka Edukasi Lingkungan**: Akses jurnal melalui tombol J memuat inventaris barang bukti, status verifikasi fakta, serta komparasi data faktual laporan Status Deforestasi Indonesia (STADI) 2025.

### 6. Sistem Dialog Konfrontasi Mandor Bercabang Konvergen (Dialogue Manager)
- **Evaluasi Logika Bukti**: Evaluasi kondisi logika mengevaluasi kombinasi variabel bukti jurnal saat pemain memulai konfrontasi akhir dengan NPC Mandor (Pak Hendra).
- **Struktur Skenario dan Variasi Akhir Permainan**:
  - **Rute 1 (`hasPermitFraud = True` dan `hasManMadeClue = True`)**: Opsi tuduhan mutlak menghasilkan **Ending A (Good Ending)**: Penghentian total operasi konsesi liar dan penahanan aktor intelektual.
  - **Rute 2 (`hasPermitFraud = True` dan `hasManMadeClue = False`)**: Opsi gugatan administrasi menghasilkan **Ending B (Neutral Ending)**: Sanksi administratif dan penahanan pekerja lapangan, namun aktor intelektual meloloskan diri.
  - **Rute 3 (`hasPermitFraud = False` dan `hasManMadeClue = True`)**: Pemain memiliki pilihan gertakan taktis yang dapat berujung pada pengakuan sebagian (Ending B) atau penyangkalan mandor (Ending C).
  - **Rute 4 (`hasPermitFraud = False` dan `hasManMadeClue = False`)**: Tanpa kelengkapan bukti materiil, interogasi gagal dan berujung pada penutupan kasus sepihak melalui **Ending C (Bad Ending)**.

### 7. Manajemen Status Permainan (Finite State Machine)
- Sistem `GameStateManager` mengelola alur status secara tersentralisasi:
  - `Exploration`: Eksplorasi aktif, kontrol navigasi, dan interaksi spasial aktif.
  - `Inspection`: Studio inspeksi bukti aktif, pergerakan karakter nonaktif, waktu dibekukan.
  - `Journal`: Antarmuka jurnal aktif, pergerakan karakter nonaktif, waktu dibekukan.
  - `Dialogue`: Sesi dialog konfrontasi aktif, interaksi percabangan aktif.
  - `Ending`: Layar hasil investigasi akhir dengan evaluasi keputusan.
  - `Menu`: Sesi konfigurasi antarmuka dan pembuka prolog sinematik.

---

## Tabel Kontrol dan Shortcut Input

| Tombol / Masukan | Status Permainan | Aksi / Fungsi |
| :--- | :--- | :--- |
| **W, A, S, D** | `Exploration` | Navigasi Karakter (Maju, Kiri, Mundur, Kanan) |
| **Mouse** | `Exploration` | Rotasi Sudut Pandang Kamera (Pitch -80 s.d. +80 Derajat) |
| **Left Shift** | `Exploration` | Berlari (*Sprint*) |
| **E** | `Exploration` | Berinteraksi dengan Bukti / Memulai Dialog dengan NPC Mandor |
| **J** | `Exploration` / `Journal` | Membuka dan Menutup Jurnal Investigasi Forensik |
| **Klik Kiri (Tahan & Seret)** | `Inspection` | Rotasi Bebas 360 Derajat Model Barang Bukti 3D |
| **Scroll Wheel Mouse** | `Inspection` | Perbesaran (*Zoom In / Zoom Out*) Model Bukti |
| **F** | `Inspection` | Mengambil dan Mengamankan Bukti ke Jurnal |
| **Q** / **Escape** | `Inspection` / `Journal` | Menutup Tampilan dan Kembali ke Mode Eksplorasi |
| **Klik Kiri / Spasi / Enter** | `Menu` (Prolog) | Memajukan Halaman Prolog / Mempercepat Efek Ketik |

---

## Struktur Direktori Script

```plaintext
Assets/Script/
├── AnomalyHotspot.cs         # Validasi titik observasi anomali bukti (dwell time)
├── CameraEffects.cs          # Simulasi prosedural head-bobbing, body sway, dan cushion
├── ClueInspector.cs          # Studio inspeksi bukti 3D sub-viewport dan bilinear blur
├── DialogueManager.cs        # Sistem narasi konvergen 4 rute skenario dan 3 ending
├── GameStateManager.cs       # Finite State Machine sentral alur status permainan
├── GreyboxEvidenceSetup.cs   # Konfigurasi lingkungan bukti investigasi dan posisi NPC
├── InspectionUI.cs           # Manajemen antarmuka HUD studio inspeksi barang bukti
├── InteractableObject.cs     # Definisi tipe bukti, jangkauan dinamis, dan metadata
├── JournalManager.cs         # Pelacak status bukti STADI 2025 dan antarmuka jurnal
├── MouseLook.cs              # Kontrol rotasi kamera first-person dan pitch clamping
├── NPCMandor.cs              # Komponen pemicu konfrontasi NPC Mandor Lapangan
├── PlayerInteract.cs         # Pemindaian interaksi center-dot raycast dan cone-cast
├── PlayerMovement.cs         # Kontroler fisika gerak, toleransi lereng, dan mud drag
├── PrologueController.cs     # Kontroler prolog sinematik, typewriter effect, dan paginasi
├── ReticleUI.cs              # Visualisasi reticle dinamis dan prompt interaktif
└── SurfaceModifier.cs        # Pengubah friksi permukaan area tanah dan lumpur
```

---

## Verifikasi dan Build Teknis

Proyek ini terverifikasi bersih tanpa kesalahan kompilasi menggunakan .NET SDK Unity:

```bash
dotnet build Assembly-CSharp.csproj
```

---

## Catatan Perubahan (Changelog)

### Versi 1.7.2 - Pengaturan Orientasi Awal Model Clue Inspector
- **Rotasi Awal Per-Clue (`InteractableObject` & `ClueInspector`)**:
  - Penambahan parameter `inspectionInitialEuler` pada `InteractableObject` untuk mengatur sudut hadap awal objek saat memasuki Mode Clue Inspector.
  - Penyesuaian otomatis orientasi awal `PROP_Clue_ExcavatorLogbook` sebesar 180° agar halaman buku log langsung tampil tegak dan mudah dibaca tanpa terbalik.

### Versi 1.7.1 - Peningkatan Kontras Visual Penanda Titik Anomali
- **Latar Gelap Kontras Tinggi pada Marker Hotspot (`ClueInspector`)**:
  - Penambahan lingkaran latar gelap semi-transparan di belakang cincin target serta *pill badge* gelap di belakang teks **`[Klik] Periksa`** agar tetap sangat jelas terbaca baik di atas objek gelap maupun permukaan kertas terang (seperti `PROP_Clue_ExcavatorLogbook`).
  - Perluasan area klik hingga mencakup *pill badge* label di bawah cincin.

### Versi 1.7.0 - Interaksi Klik Titik Anomali & Peningkatan Zoom Clue Inspector
- **Penanda Visual & Klik Titik Anomali (`ClueInspector` & `AnomalyHotspot`)**:
  - Setiap titik `AnomalyHotspot` yang menghadap kamera kini menampilkan penanda cincin berdenyut (*pulsing target pin*) lengkap dengan petunjuk **`Klik untuk periksa`**.
  - Pemain dapat langsung mengklik penanda tersebut untuk memverifikasi anomali dan memicu monolog batin tanpa harus menatap tengah layar secara pasif.
  - Penambahan indikator status **`Terperiksa`** berwarna hijau setelah titik anomali diklik.
- **Peningkatan Batas Zoom In Objek Kecil (`ClueInspector`)**:
  - Penurunan `minZoomRatio` ke `0.28x`, `minZoomDistance` minimum ke `0.18m`, dan `nearClipPlane` kamera inspeksi ke `0.02m` dengan sensitivitas scroll proporsional agar detail tulisan kecil (seperti `PROP_Clue_ExcavatorLogbook`) dapat di-*zoom* jauh lebih dekat dan jelas.

### Versi 1.6.2 - Perbaikan Deteksi Collider & Interaksi Clue Tipis
- **Auto-Collider pada `InteractableObject`**:
  - Penambahan `EnsureColliderExists()` saat `Awake()` agar prop clue tipis (seperti `PROP_Clue_ExcavatorLogbook`) yang belum memiliki collider fisik otomatis dibuatkan `BoxCollider` sesuai batas mesh-nya.
- **Toleransi Permukaan Tipis pada `PlayerInteract`**:
  - Penambahan toleransi jarak `0.15m` pada *SphereCast* cadangan agar objek tipis yang diletakkan rata di atas tunggul atau meja tetap mudah dideteksi tanpa terhalang permukaan di bawahnya.

### Versi 1.6.1 - Revisi Sekuens Narasi Zona Kabut & Jeda Monolog Pasca-Radio
- **Pemisahan Monolog Batin & Jeda Pasca-Radio (`RadioDialogueManager`)**:
  - Penambahan properti `postDelay` per baris dialog agar transisi antar-dialog mendukung jeda sinematik, serta otomatis menurunkan walkie-talkie selama jeda berlangsung.
  - Pembaruan alur `PlayFogZoneDialogue()` pada `Zona_Kabut_Tebal`: Monolog pembuka *(Batuk-batuk)* -> Nadia (Via Radio) -> Rangga -> jeda **3,5 detik** -> Monolog Batin Rangga mengenai kondisi sungai kering, tumpahan solar hitam, dan pembakaran semak.

### Versi 1.6.0 - Alur Investigasi Clue Lanjutan, Zona Kabut & Peningkatan Terrain
- **Mekanik Investigasi & Interaksi Clue (`InteractableObject` & `ClueInspector`)**:
  - Penambahan fitur `Require Orbit Before Interact` yang mewajibkan pemain berjalan mengelilingi objek terlebih dahulu sebelum interaksi `[E]` terbuka, lengkap dengan indikator progres persentase pada `ReticleUI`.
  - Penambahan opsi `Trigger Monologue After Rotate` dan `Discovery Monologue` pada `AnomalyHotspot` untuk memicu monolog batin Rangga setelah memutar objek atau menemukan titik anomali.
  - Penambahan opsi `Disable After Inspection` agar clue yang selesai diperiksa tidak dapat di-interact ulang.
  - Penataan ulang tata letak UI `ClueInspector` dan `InspectionUI` (memindahkan status `[ANOMALI TERVERIFIKASI]` ke bagian atas layar) agar tidak bertumpuk dengan teks monolog di bawah.
- **Narasi `CLUE-02` (Tunggul Eboni) & `Zona_Kabut_Tebal`**:
  - Penambahan tipe `Clue02TunggulEboni` pada `DialogueTriggerZone` untuk memicu percakapan radio Rangga & Nadia melalui `BoxCollider` trigger di area hamparan tunggul kayu.
  - Integrasi otomatis monolog batin Rangga dan dialog radio Nadia pada `FogZone` (`Zona_Kabut_Tebal`) saat pemain memasuki zona kabut asap tebal.
- **Perbaikan Shader Jalan Setapak & Stabilitas Console**:
  - Penambahan tag `TerrainCompatible` dan `SplatCount` pada `LowPolyTerrain.shader`, serta peningkatan sensitivitas dan keterlihatan tekstur Layer 1 (`_Splat1`) untuk jalan setapak di pencahayaan gelap.
  - Perbaikan warning `AudioSource` pada `OnValidate` (`RoofWaterDripEmitter`) dan fallback `MeshCollider` non-convex (`InteractableObject`).

### Versi 1.5.0 - Sistem Walkie-Talkie Prosedural & Komunikasi Radio
- **Kontroler Walkie-Talkie (`WalkieTalkieController`)**:
  - Kemunculan otomatis objek `PROP_Comms_WalkieTalkie` di tangan pemain saat komunikasi radio berlangsung (dan tersembunyi saat eksplorasi biasa, monolog batin, maupun mode inspeksi).
  - Animasi prosedural mendekat ke kamera saat Rangga berbicara, menjauh sedikit saat mendengarkan Nadia, serta ayunan tangan (*hand sway*) dan napas halus.
  - Fitur simpan pose visual langsung dari Inspector (*WYSIWYG Context Menu*) serta dukungan *Anchor Transform* opsional.
- **Sinkronisasi Status Pembicara (`RadioDialogueManager`)**:
  - Penambahan event `OnRadioStateChanged` (`None`, `Rangga`, `Nadia`) untuk menggerakkan animasi walkie-talkie secara sinkron dengan dialog.

### Versi 1.4.0 - Atmosfer Hutan, Partikel Ambien Mikro & Polish Inspeksi 3D
- **Partikel Ambien Mikro & Material Permanen**:
  - Spora/debu hutan reaktif terhadap sorotan senter (`AmbientDustMotes` + `DustMoteParticle.shader`).
  - Kunang-kunang malam di sekitar semak dan lampu pos jaga (`FireflyEmitter` + `SoftGlowParticle.shader`).
  - Tetesan air atap seng pos jaga dengan audio 3D (`RoofWaterDripEmitter`) yang dikalibrasi ritmenya agar lebih atmosferik.
- **Volumetric God Rays, Lampu Berayun & Jarak Render Hutan**:
  - Integrasi berkas cahaya volumetrik (`VolumetricLightShaft`) dan animasi pendulum lampu gantung pos jaga (`SwingingHangingLamp`).
  - Penghapusan kerucut kabut putih pada senter agar pandangan pemain tidak terhalang.
  - Peningkatan jarak render pohon billboard (`500m`) dan batas LOD mesh (`TerrainVisibilityEnhancer`) agar hutan tetap lebat dari kejauhan.
- **Polish Mode Clue Inspector 3D**:
  - Pemisahan rotasi model dari kamera agar objek dapat diputar bebas, animasi transisi masuk/keluar, serta pembatasan jarak maksimal *zoom out* (`maxZoomRatio`) agar objek tetap proporsional.
  - Pemusatan titik gizmo dan deteksi jarak `InteractableObject` ke tengah collider/targetPivot.

### Versi 1.3.1 - Pembaruan Polish Alur & Kontrol Narasi Prolog
- **Mekanik Tahan Klik untuk Akselerasi Ketik (Hold to Accelerate)**:
  - Pemain dapat menahan klik kiri mouse atau tombol Spasi untuk mempercepat animasi kemunculan kata (~5x lebih cepat).
  - Menghilangkan instan-skip saat klik biasa agar ritme dan atmosfer cerita pembuka tetap terjaga.
- **Stabilisasi Tata Letak Vertikal (Top-Aligned Typewriter)**:
  - Mengunci perataan teks vertikal ke bagian atas (*Top Alignment*) untuk mengeliminasi pergeseran teks ke atas saat baris baru muncul.
- **Pembersihan Arsitektur & Debounce Frame**:
  - Penambahan proteksi frame ganda (*same-frame input guard*) untuk memastikan transisi antarhalaman narasi berlangsung mulus tanpa loncatan ganda.

### Versi 1.3.0 - Pembaruan Sistem Prolog Sinematik dan Efek Transisi
- **Efek Ketik Transisional (Typewriter Transition)**:
  - Implementasi animasi ketik karakter per karakter menggunakan properti `maxVisibleCharacters` pada TextMeshPro.
  - Parameter konfigurasi `typingSpeed` (kecepatan ketik detik per karakter) dan dukungan `typingSound` opsional.
- **Jeda Awal Layar Hitam (Initial Black Screen Delay)**:
  - Penambahan parameter `initialBlackDelay` pada `PrologueController`. Saat scene dimuat, panel hitam polos tampil terlebih dahulu sebelum teks judul dan narasi diaktifkan.
  - Penambahan opsi `allowSkipDelayWithClick` yang memungkinkan pemain melewati jeda awal dengan klik mouse.
- **Dukungan Struktur Hierarchy Fleksibel**:
  - Penambahan fitur deteksi otomatis untuk susunan objek narasi terpisah pada Hierarchy (`Narasi`, `Narasi (1)`, `Narasi (2)`, dst.).
  - Dukungan fallback untuk mode satu komponen TextMeshPro terpadu (`singlePrologueText`).
  - Penambahan proteksi debounce (*input cooldown*) untuk mencegah loncatan halaman ganda saat klik beruntun.

### Versi 1.2.0 - Pembaruan Integrasi Jurnal STADI 2025 dan Dialog Konvergen Mandor
- **Sistem Jurnal Investigasi Otomatis**:
  - Pengenalan pelacakan variabel boolean forensik: `hasPermitFraud`, `hasManMadeClue`, dan `hasNaturalClue`.
  - Integrasi antarmuka jurnal edukatif berbasis data Status Deforestasi Indonesia (STADI) 2025.
- **Sistem Narasi Konvergen NPC Mandor (Pak Hendra)**:
  - Evaluasi logika kombinasi bukti menghasilkan 4 rute skenario interogasi dan 3 variasi ending (Ending A, Ending B, Ending C).
  - Penataan posisi NPC Mandor dan dokumen izin konsesi pada scene eksplorasi `Gameplay.unity`.
- **Integrasi Finite State Machine**:
  - Penambahan modul `GameStateManager` untuk sinkronisasi perpindahan status antara `Exploration`, `Inspection`, `Journal`, `Dialogue`, `Ending`, dan `Menu`.

### Versi 1.1.0 - Pembaruan Lingkungan Visual dan Shader Medan
- **Visualisasi Lanskap Hutan Rano Towa**:
  - Kalibrasi palet warna Low-Poly Terrain menjadi nuansa hijau tua alami kawasan hutan tropis basah.
  - Penyesuaian fungsi pencahayaan shader kustom pada `CustomLighting.hlsl`.
  - Integrasi komponen `SurfaceModifier` pada area kubangan alat berat untuk mereduksi kecepatan gerak sebesar 35 persen.

### Versi 1.0.0 - Rilis Fondasi Inti (Core Foundation)
- **Pengontrol Karakter First-Person**:
  - Sistem pergerakan berbasis CharacterController dengan penanganan lereng, gravitasi, dan lari cepat (*sprint*).
  - Kamera sudut pandang orang pertama dengan batasan pitch vertikal dan efek ayunan prosedural (*head bobbing*).
- **Sistem Deteksi Spasial dan Reticle**:
  - Implementasi raycast titik tengah dan pemindaian kerucut pandang 30 derajat dengan deteksi jarak permukaan.
  - Visualisasi reticle dinamis dengan teks prompt kontekstual interaktif.
- **Studio Inspeksi Bukti Tiga Dimensi**:
  - Penampil model bukti terisolasi dengan rotasi 360 derajat, perbesaran bertahap, dan pengaburan latar belakang *bilinear blur*.
  - Integrasi titik pengamatan anomali (*Anomaly Hotspots*) berbasis validasi durasi tatap (*dwell time*).
