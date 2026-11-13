public partial class CharacterInfo
{
    public WeaponInfo EquippedWeapon { get; private set; }

    public void EquipWeapon(WeaponInfo weapon)
    {
        if (weapon == null)
            return;

        EquippedWeapon = weapon;
    }

    public void UnEquipWeapon()
    {
        EquippedWeapon = null;
        if (EquippedItem != null && EquippedItem.Type == ItemType.Weapon)
            EquippedItem = null;
    }
}
