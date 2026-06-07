/// <summary>
/// Oyunun genel aşamalarını tanımlar.
/// Deployment: Gemilerin hex'lere yerleştirildiği konuşlandırma fazı
/// Combat: Sıra tabanlı muharebe fazı
/// Resolution: Savaş sonucu, ödüller ve kayıpların işlendiği faz
/// </summary>
public enum GamePhase
{
    Deployment,
    Combat,
    Resolution
}

/// <summary>
/// Oyuncu kimliğini tanımlar. 1v1 yapıda iki oyuncu.
/// </summary>
public enum PlayerId
{
    None,
    Player1,
    Player2
}

/// <summary>
/// Gemi tipi. Her tip farklı base stat'lara sahiptir.
/// </summary>
public enum ShipType
{
    Frigate,     // Firkateyn — hızlı keşif gemisi
    Destroyer,   // Muhrip — dengeli savaş gemisi
    Cruiser,     // Kruvazör — ağır ateş gücü
    Battleship,  // Zırhlı — yavaş ama dayanıklı tank
    Submarine    // Denizaltı — yüksek hasar, düşük HP
}

/// <summary>
/// Bir geminin tur içindeki aksiyon durumu.
/// Her tur başında Idle'a sıfırlanır.
/// </summary>
public enum ShipActionState
{
    Idle,       // Henüz aksiyon yapmadı
    Moved,      // Hareket etti, hâlâ saldırabilir
    Attacked,   // Saldırdı, hâlâ hareket edebilir
    Done        // Tüm aksiyonlarını tüketti
}
