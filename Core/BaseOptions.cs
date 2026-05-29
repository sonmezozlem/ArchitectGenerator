namespace ArchitectGenerator.Core;

/// <summary>
/// Üretilecek solution'ın base/auth katmanı için kullanıcı tarafından seçilen ayarlar.
/// </summary>
public sealed class BaseOptions
{
	/// <summary>
	/// Rol isimleri. İlk eleman en yetkili (Admin) roldür, son eleman en düşük yetkili roldür.
	/// </summary>
	public required IReadOnlyList<string> Roles { get; init; }

	/// <summary>
	/// Public (herkese açık) self-register endpoint'i üretilsin mi?
	/// Üretilirse kullanıcı rolünü seçemez; her zaman <see cref="SelfRegisterRole"/> atanır.
	/// </summary>
	public required bool PublicRegister { get; init; }

	/// <summary>En yetkili rol (kullanıcı oluşturma endpoint'i bu rolle korunur).</summary>
	public string AdminRole => Roles[0];

	/// <summary>Public self-register'da atanacak en düşük yetkili rol.</summary>
	public string SelfRegisterRole => Roles[^1];
}
