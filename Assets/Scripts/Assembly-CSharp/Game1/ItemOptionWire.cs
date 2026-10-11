namespace Game1
{
    // Enabled only after the server explicitly acknowledges the capability.
    public static class ItemOptionWire
    {
        public static bool UseInt32;
        public static string FormatGridQuantity(int quantity)
        {
            return quantity >= 1000000 ? Res.formatNumber(quantity) : quantity.ToString();
        }
        public static int ReadPickupQuantity(myReader reader)
        {
            int legacyQuantity = reader.readShort();
            return UseInt32 && reader.available() == 4 ? reader.readInt() : legacyQuantity;
        }
        public static int ReadParam(myReader reader)
        {
            return UseInt32 ? reader.readInt() : reader.readUnsignedShort();
        }
    }
}
