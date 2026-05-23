# Tubes_TibaTibaJadiKelompok
## IF25-21013 Strategi Algoritma — Tugas Besar 

---

## i. Penjelasan Singkat Algoritma Greedy Setiap Bot

### Bot Utama — TibaTibaJadiKelompok
Strategi Greedy: Bot bergerak menyusuri pinggir arena (wall hugging) 
dan hanya menembak ketika sudut antara arah gun dengan posisi musuh 
kurang dari 3 derajat.  
Heuristic: Akurasi maksimum — setiap peluru yang ditembakkan hampir 
pasti mengenai target, meminimalkan pemborosan energi dan 
memaksimalkan Bullet Damage.

### Alt Bot 1 — AggressiveHunter
Strategi Greedy: Bot selalu memilih dan mengunci musuh dengan energi 
terendah sebagai target, sambil bergerak membentuk pola angka 8 
agar susah ditembak.  
Heuristic: Target energy minimum — musuh dengan energi terendah 
paling mudah dibunuh sehingga memaksimalkan peluang mendapat 
Bullet Damage Bonus sebesar 20%.

### Alt Bot 2 — EnergyManager
Strategi Greedy: Bot selalu menghitung dan memilih firepower yang 
paling efisien berdasarkan jarak ke musuh setiap kali menembak,
sambil bergerak zigzag yang tenang dan terukur.  
Heuristic: Firepower optimal = min(3.0, 500/jarak) — memaksimalkan 
damage yang dihasilkan per satuan energi yang dikeluarkan.

### Alt Bot 3 — ClosestTarget
Strategi Greedy: Bot selalu mengejar dan menabrak musuh yang paling 
dekat dari posisinya saat ini.  
Heuristic: Jarak minimum — musuh terdekat memberikan hit rate 
tertinggi sekaligus memaksimalkan peluang Ram Damage Bonus 
sebesar 30%.

---

## ii. Requirement Program

- **Java** versi 17 atau lebih baru (untuk menjalankan game engine)
- **.NET SDK** versi 10.0 atau lebih baru (untuk build dan run bot C#)

### Cara Cek Requirement
java -version
dotnet --version

---

## iii. Command Build dan Menjalankan Program
cd C:\RobocodeITERA
java -jar robocode-tankroyale-gui-0.30.0.jar

### Menjalankan Game Engine
cd src/main-bot
TibaTibaJadiKelompok.cmd

### Menjalankan Alt Bot
cd src/alternative-bots/alt-bot-1
AggressiveHunter.cmd
cd src/alternative-bots/alt-bot-2
EnergyManager.cmd
cd src/alternative-bots/alt-bot-3
ClosestTarget.cmd

### Kendala yang Dihadapi Saat Development
1. Bot sering stuck saat menabrak dinding karena penggunaan 
   blocking commands (Forward, TurnRight). Solusi: mengganti 
   semua perintah gerak dengan Set-commands non-blocking 
   (SetForward, SetTurnRight) dan menambahkan state machine 
   untuk mengatur gerakan per tick.

2. Bot tidak terdeteksi oleh game engine saat di-boot karena 
   nama class di file .cs tidak sesuai dengan nama file .json 
   dan .csproj. Solusi: memastikan semua nama file dan nama 
   class konsisten.

3. Port 7654 sudah terpakai saat mencoba menjalankan game engine 
   kembali. Solusi: matikan proses lama dengan perintah 
   taskkill /F /IM java.exe atau restart komputer.

4. Versi TargetFramework di file .csproj tidak sesuai dengan 
   versi .NET yang terinstall. Solusi: sesuaikan nilai 
   TargetFramework dengan output dari perintah dotnet --version.

---

## iv. Author

| Nama | NIM |
|------|-----|
| Mochamad Rasya Khairiza | 124140002 |
| Maulana Muhammad Rizki | 124140008 |
| Krisna Dwi Cristian | 124140152 |

*Kelompok:* TibaTibaJadiKelompok <br>
*Mata Kuliah:* IF25-21013 Strategi Algoritma <br>
*Semester:* Genap 2026/2027 <br>
*Dosen:* Winda Yulita, M.Cs