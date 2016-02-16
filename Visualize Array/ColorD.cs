using System;
using System.Collections.Generic;
using System.Text;
#if !SILVERLIGHT
using System.Drawing;
#endif
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ScottsUtils
{
    [DebuggerDisplay("\\{ARGB = ({A}, {R}, {G}, {B})\\}")]
    public struct ColorD
    {
        double m_R;
        double m_G;
        double m_B;
        double m_A;

        public override string ToString()
        {
            return string.Format(
                "ColorRGB [A={0}, R={1}, G={2}, B={3}]",
                A, R, G, B);
        }

        /// <summary>
        /// Red component of color.  0.0 to 1.0
        /// </summary>
        public double R
        {
            get
            {
                return m_R;
            }
        }

        /// <summary>
        /// Green component of color.  0.0 to 1.0
        /// </summary>
        public double G
        {
            get
            {
                return m_G;
            }
        }

        /// <summary>
        /// Blue component of color.  0.0 to 1.0
        /// </summary>
        public double B
        {
            get
            {
                return m_B;
            }
        }

        /// <summary>
        /// Alpha component of color.  0.0 to 1.0.  0.0 is transparent, 1.0 is opaque.
        /// </summary>
        public double A
        {
            get
            {
                return m_A;
            }
        }

        static Dictionary<string, string> s_Colors = null;
        static Dictionary<string, string> s_OtherColors = null;

        public static void AddOtherColor(string name, string hex)
        {
            s_Colors = null;
            if (s_OtherColors == null)
            {
                s_OtherColors = new Dictionary<string, string>();
            }

            name = name.Replace(" ", "");
            name = name.Replace("-", "");
            name = name.Replace("'", "");
            name = name.ToLower();

            s_OtherColors.Add(name, hex.ToUpper());
        }

        public static List<NamedColor> GetAllColors()
        {
            List<NamedColor> ret = new List<NamedColor>();

            #region Web Colors
            Make(ret, 
                "Alice Blue", "F0F8FF", "Antique White", "FAEBD7", "Aqua", "00FFFF", "Aquamarine", "7FFFD4",
                "Azure", "F0FFFF", "Beige", "F5F5DC", "Bisque", "FFE4C4", "Black", "000000",
                "Blanched Almond", "FFEBCD", "Blue", "0000FF", "Blue Violet", "8A2BE2", "Brown", "A52A2A",
                "Burly Wood", "DEB887", "Cadet Blue", "5F9EA0", "Chartreuse", "7FFF00", "Chocolate", "D2691E",
                "Coral", "FF7F50", "Cornflower Blue", "6495ED", "Cornsilk", "FFF8DC", "Crimson", "DC143C",
                "Cyan", "00FFFF", "Dark Blue", "00008B", "Dark Cyan", "008B8B", "Dark Golden Rod", "B8860B",
                "Dark Gray", "A9A9A9", "Dark Green", "006400", "Dark Grey", "A9A9A9", "Dark Khaki", "BDB76B",
                "Dark Magenta", "8B008B", "Dark Olive Green", "556B2F", "Dark Orange", "FF8C00",
                "Dark Orchid", "9932CC", "Dark Red", "8B0000", "Dark Salmon", "E9967A", "Dark Sea Green", "8FBC8F",
                "Dark Slate Blue", "483D8B", "Dark Slate Gray", "2F4F4F", "Dark Slate Grey", "2F4F4F",
                "Dark Turquoise", "00CED1", "Dark Violet", "9400D3", "Deep Pink", "FF1493",
                "Deep Sky Blue", "00BFFF", "Dim Gray", "696969", "Dim Grey", "696969", "Dodger Blue", "1E90FF",
                "Fire Brick", "B22222", "Floral White", "FFFAF0", "Forest Green", "228B22", "Fuchsia", "FF00FF",
                "Gainsboro", "DCDCDC", "Ghost White", "F8F8FF", "Gold", "FFD700", "Golden Rod", "DAA520",
                "Gray", "808080", "Green", "008000", "Green Yellow", "ADFF2F", "Grey", "808080",
                "Honey Dew", "F0FFF0", "Hot Pink", "FF69B4", "Indian Red ", "CD5C5C", "Indigo ", "4B0082",
                "Ivory", "FFFFF0", "Khaki", "F0E68C", "Lavender", "E6E6FA", "Lavender Blush", "FFF0F5",
                "Lawn Green", "7CFC00", "Lemon Chiffon", "FFFACD", "Light Blue", "ADD8E6", "Light Coral", "F08080",
                "Light Cyan", "E0FFFF", "Light Golden Rod Yellow", "FAFAD2", "Light Gray", "D3D3D3",
                "Light Green", "90EE90", "Light Grey", "D3D3D3", "Light Pink", "FFB6C1", "Light Salmon", "FFA07A",
                "Light Sea Green", "20B2AA", "Light Sky Blue", "87CEFA", "Light Slate Gray", "778899",
                "Light Slate Grey", "778899", "Light Steel Blue", "B0C4DE", "Light Yellow", "FFFFE0",
                "Lime", "00FF00", "Lime Green", "32CD32", "Linen", "FAF0E6", "Magenta", "FF00FF",
                "Maroon", "800000", "Medium Aqua Marine", "66CDAA", "Medium Blue", "0000CD",
                "Medium Orchid", "BA55D3", "Medium Purple", "9370DB", "Medium Sea Green", "3CB371",
                "Medium Slate Blue", "7B68EE", "Medium Spring Green", "00FA9A", "Medium Turquoise", "48D1CC",
                "Medium Violet Red", "C71585", "Midnight Blue", "191970", "Mint Cream", "F5FFFA",
                "Misty Rose", "FFE4E1", "Moccasin", "FFE4B5", "Navajo White", "FFDEAD", "Navy", "000080",
                "Old Lace", "FDF5E6", "Olive", "808000", "Olive Drab", "6B8E23", "Orange", "FFA500",
                "Orange Red", "FF4500", "Orchid", "DA70D6", "Pale Golden Rod", "EEE8AA", "Pale Green", "98FB98",
                "Pale Turquoise", "AFEEEE", "Pale Violet Red", "DB7093", "Papaya Whip", "FFEFD5",
                "Peach Puff", "FFDAB9", "Peru", "CD853F", "Pink", "FFC0CB", "Plum", "DDA0DD",
                "Powder Blue", "B0E0E6", "Purple", "800080", "Rebecca Purple", "663399", "Red", "FF0000",
                "Rosy Brown", "BC8F8F", "Royal Blue", "4169E1", "Saddle Brown", "8B4513", "Salmon", "FA8072",
                "Sandy Brown", "F4A460", "Sea Green", "2E8B57", "Sea Shell", "FFF5EE", "Sienna", "A0522D",
                "Silver", "C0C0C0", "Sky Blue", "87CEEB", "Slate Blue", "6A5ACD", "Slate Gray", "708090",
                "Slate Grey", "708090", "Snow", "FFFAFA", "Spring Green", "00FF7F", "Steel Blue", "4682B4",
                "Tan", "D2B48C", "Teal", "008080", "Thistle", "D8BFD8", "Tomato", "FF6347", "Turquoise", "40E0D0",
                "Violet", "EE82EE", "Wheat", "F5DEB3", "White", "FFFFFF", "White Smoke", "F5F5F5",
                "Yellow", "FFFF00", "Yellow Green", "9ACD32");
            #endregion
            #region Other colors
            Make(ret,
                "Absolute Zero", "0048BA", "Acid Green", "B0BF1A", "Aero", "7CB9E8", "Aero Blue", "C9FFE5",
                "African Violet", "B284BE", "Air Force Blue", "00308F", "Air Superiority Blue", "72A0C1",
                "Alabama Crimson", "AF002A", "Alien Armpit", "84DE02", "Alizarin Crimson", "E32636",
                "Alloy Orange", "C46210", "Almond", "EFDECD", "Amaranth", "E52B50",
                "Amaranth Deep Purple", "AB274F", "Amaranth Pink", "F19CBB", "Amaranth Purple", "AB274F",
                "Amaranth Red", "D3212D", "Amazon", "3B7A57", "Amber", "FFBF00", "American Rose", "FF033E",
                "Amethyst", "9966CC", "Amsterdam Red", "BC0031", "Android Green", "A4C639",
                "Anti-Flash White", "F2F3F4", "Antique Brass", "CD9575", "Antique Bronze", "665D1E",
                "Antique Fuchsia", "915C83", "Antique Ruby", "841B2D", "Ao", "008000", "Apple Green", "8DB600",
                "Apricot", "FBCEB1", "Arctic Lime", "D0FF14", "Army Green", "4B5320", "Arsenic", "3B444B",
                "Artichoke", "8F9779", "Arylide Yellow", "E9D66B", "Ash Grey", "B2BEB5", "Asparagus", "87A96B",
                "Atomic Tangerine", "FF9966", "Auburn", "A52A2A", "Audrey", "A2D4E7", "Aureolin", "FDEE00",
                "AuroMetalSaurus", "6E7F80", "Avocado", "568203", "Aztec Gold", "C39953", "Azure Mist", "F0FFFF",
                "Azureish White", "DBE9F4", "Baby Blue", "89CFF0", "Baby Blue Eyes", "A1CAF1",
                "Baby Pink", "F4C2C2", "Baby Powder", "FEFEFA", "Baker-Miller Pink", "FF91AF",
                "Ball Blue", "21ABCD", "Banana Mania", "FAE7B5", "Banana Yellow", "FFE135",
                "Bangladesh Green", "006A4E", "Barbie Pink", "E0218A", "Barn Red", "7C0A02",
                "Battleship Grey", "848482", "Bazaar", "98777B", "Beau Blue", "BCD4E6", "Beaver", "9F8170",
                "B'dazzled Blue", "2E5894", "Big Dip O'ruby", "9C2542", "Big Foot Feet", "E88E5A",
                "Bistre", "3D2B1F", "Bistre Brown", "967117", "Bitter Lemon", "CAE00D", "Bitter Lime", "BFFF00",
                "Bittersweet", "FE6F5E", "Bittersweet Shimmer", "BF4F51", "Black Bean", "3D0C02",
                "Black Coral", "54626F", "Black Leather Jacket", "253529", "Black Olive", "3B3C36",
                "Black Shadows", "BFAFB2", "Blast-Off Bronze", "A57164", "Bleu De France", "318CE7",
                "Blizzard Blue", "ACE5EE", "Blond", "FAF0BE", "Blue Bell", "A2A2D0", "Blue-Gray", "6699CC",
                "Blue-Green", "0D98BA", "Blue Jeans", "5DADEC", "Blue Lagoon", "ACE5EE",
                "Blue-Magenta Violet", "553592", "Blue Sapphire", "126180", "Blue Yonder", "5072A7",
                "Blueberry", "4F86F7", "Bluebonnet", "1C1CF0", "Blush", "DE5D83", "Bole", "79443B",
                "Bondi Blue", "0095B6", "Bone", "E3DAC9", "Booger Buster", "DDE26A",
                "Boston University Red", "CC0000", "Bottle Green", "006A4E", "Boysenberry", "873260",
                "Brandeis Blue", "0070FF", "Brass", "B5A642", "Brick Red", "CB4154", "Bright Cerulean", "1DACD6",
                "Bright Green", "66FF00", "Bright Lavender", "BF94E4", "Bright Lilac", "D891EF",
                "Bright Maroon", "C32148", "Bright Navy Blue", "1974D2", "Bright Pink", "FF007F",
                "Bright Turquoise", "08E8DE", "Bright Ube", "D19FE8", "Bright Yellow", "FFAA1D",
                "Brilliant Azure", "3399FF", "Brilliant Lavender", "F4BBFF", "Brilliant Rose", "FF55A3",
                "Brink Pink", "FB607F", "British Racing Green", "004225", "Bronze", "CD7F32",
                "Bronze Yellow", "737000", "Brown-Nose", "6B4423", "Brown Sugar", "AF6E4D",
                "Brown Yellow", "cc9966", "Brunswick Green", "1B4D3E", "Bubble Gum", "FFC1CC", "Bubbles", "E7FEFF",
                "Bud Green", "7BB661", "Buff", "F0DC82", "Bulgarian Rose", "480607", "Burgundy", "800020",
                "Burnished Brown", "A17A74", "Burnt Orange", "CC5500", "Burnt Sienna", "E97451",
                "Burnt Umber", "8A3324", "Byzantine", "BD33A4", "Byzantium", "702963", "Cadet", "536872",
                "Cadet Grey", "91A3B0", "Cadmium Green", "006B3C", "Cadmium Orange", "ED872D",
                "Cadmium Red", "E30022", "Cadmium Yellow", "FFF600", "Cafe Au Lait", "A67B5B",
                "Cafe Noir", "4B3621", "Cal Poly Green", "1E4D2B", "Cambridge Blue", "A3C1AD", "Camel", "C19A6B",
                "Cameo Pink", "EFBBCC", "Camouflage Green", "78866B", "Canary Yellow", "FFEF00",
                "Candy Apple Red", "FF0800", "Candy Pink", "E4717A", "Capri", "00BFFF", "Caput Mortuum", "592720",
                "Cardinal", "C41E3A", "Caribbean Green", "00CC99", "Carmine", "960018", "Carmine Pink", "EB4C42",
                "Carmine Red", "FF0038", "Carnation Pink", "FFA6C9", "Carnelian", "B31B1B",
                "Carolina Blue", "56A0D3", "Carrot Orange", "ED9121", "Castleton Green", "00563F",
                "Catalina Blue", "062A78", "Catawba", "703642", "Cedar Chest", "C95A49", "Ceil", "92A1CF",
                "Celadon", "ACE1AF", "Celadon Blue", "007BA7", "Celadon Green", "2F847C", "Celeste", "B2FFFF",
                "Celestial Blue", "4997D0", "Cerise", "DE3163", "Cerise Pink", "EC3B83", "Cerulean", "007BA7",
                "Cerulean Blue", "2A52BE", "Cerulean Frost", "6D9BC3", "CG Blue", "007AA5", "CG Red", "E03C31",
                "Chamoisee", "A0785A", "Champagne", "F7E7CE", "Charcoal", "36454F", "Charleston Green", "232B2B",
                "Charm Pink", "E68FAC", "Cherry", "DE3163", "Cherry Blossom Pink", "FFB7C5", "Chestnut", "954535",
                "China Pink", "DE6FA1", "China Rose", "A8516E", "Chinese Red", "AA381E",
                "Chinese Violet", "856088", "Chlorophyll Green", "4AFF00", "Chrome Yellow", "FFA700",
                "Cinereous", "98817B", "Cinnabar", "E34234", "Cinnamon", "D2691E", "Cinnamon Satin", "CD607E",
                "Citrine", "E4D00A", "Citron", "9FA91F", "Claret", "7F1734", "Classic Rose", "FBCCE7",
                "Cobalt Blue", "0047AB", "Cocoa Brown", "D2691E", "Coconut", "965A3E", "Coffee", "6F4E37",
                "Columbia Blue", "C4D8E2", "Congo Pink", "F88379", "Cool Black", "002E63", "Cool Grey", "8C92AC",
                "Copper", "B87333", "Copper Penny", "AD6F69", "Copper Red", "CB6D51", "Copper Rose", "996666",
                "Coquelicot", "FF3800", "Coral Pink", "F88379", "Coral Red", "FF4040", "Cordovan", "893F45",
                "Corn", "FBEC5D", "Cornell Red", "B31B1B", "Cosmic Cobalt", "2E2D88", "Cosmic Latte", "FFF8E7",
                "Coyote Brown", "81613e", "Cotton Candy", "FFBCD9", "Cream", "FFFDD0", "Crimson Glory", "BE0032",
                "Crimson Red", "990000", "Cultured", "F5F5F5", "Cyan Azure", "4E82b4", "Cyan-Blue Azure", "4682BF",
                "Cyan Cobalt Blue", "28589C", "Cyan Cornflower Blue", "188BC2", "Cyber Grape", "58427C",
                "Cyber Yellow", "FFD300", "Cyclamen", "F56FA1", "Daffodil", "FFFF31", "Dandelion", "F0E130",
                "Dark Blue-Gray", "666699", "Dark Brown", "654321", "Dark Brown-Tangelo", "88654E",
                "Dark Byzantium", "5D3954", "Dark Candy Apple Red", "A40000", "Dark Cerulean", "08457E",
                "Dark Chestnut", "986960", "Dark Coral", "CD5B45", "Dark Electric Blue", "536878",
                "Dark Gunmetal", "1F262A", "Dark Imperial Blue", "00416A", "Dark Jungle Green", "1A2421",
                "Dark Lava", "483C32", "Dark Lavender", "734F96", "Dark Liver", "534B4F",
                "Dark Medium Gray", "A9A9A9", "Dark Midnight Blue", "003366", "Dark Moss Green", "4A5D23",
                "Dark Pastel Blue", "779ECB", "Dark Pastel Green", "03C03C", "Dark Pastel Purple", "966FD6",
                "Dark Pastel Red", "C23B22", "Dark Pink", "E75480", "Dark Powder Blue", "003399",
                "Dark Puce", "4F3A3C", "Dark Purple", "301934", "Dark Raspberry", "872657",
                "Dark Scarlet", "560319", "Dark Sienna", "3C1414", "Dark Sky Blue", "8CBED6",
                "Dark Spring Green", "177245", "Dark Tan", "918151", "Dark Tangerine", "FFA812",
                "Dark Taupe", "483C32", "Dark Terra Cotta", "CC4E5C", "Dark Vanilla", "D1BEA8",
                "Dark Yellow", "9B870C", "Dartmouth Green", "00703C", "Davy's Grey", "555555",
                "Debian Red", "D70A53", "Deep Aquamarine", "40826D", "Deep Carmine", "A9203E",
                "Deep Carmine Pink", "EF3038", "Deep Carrot Orange", "E9692C", "Deep Cerise", "DA3287",
                "Deep Champagne", "FAD6A5", "Deep Chestnut", "B94E48", "Deep Coffee", "704241",
                "Deep Fuchsia", "C154C1", "Deep Green", "056608", "Deep Green-Cyan Turquoise", "0E7C61",
                "Deep Jungle Green", "004B49", "Deep Koamaru", "333366", "Deep Lemon", "F5C71A",
                "Deep Lilac", "9955BB", "Deep Magenta", "CC00CC", "Deep Maroon", "820000", "Deep Mauve", "D473D4",
                "Deep Moss Green", "355E3B", "Deep Peach", "FFCBA4", "Deep Puce", "A95C68", "Deep Red", "850101",
                "Deep Ruby", "843F5B", "Deep Saffron", "FF9933", "Deep Space Sparkle", "4A646C",
                "Deep Spring Bud", "556B2F", "Deep Taupe", "7E5E60", "Deep Tuscan Red", "66424D",
                "Deep Violet", "330066", "Deer", "BA8759", "Denim", "1560BD", "Denim Blue", "2243B6",
                "Desaturated Cyan", "669999", "Desert", "C19A6B", "Desert Sand", "EDC9AF", "Desire", "EA3C53",
                "Diamond", "B9F2FF", "Dingy Dungeon", "C53151", "Dirt", "9B7653", "Dogwood Rose", "D71868",
                "Dollar Bill", "85BB65", "Donkey Brown", "664C28", "Drab", "967117", "Duke Blue", "00009C",
                "Dust Storm", "E5CCC9", "Dutch White", "EFDFBB", "Earth Yellow", "E1A95F", "Ebony", "555D50",
                "Ecru", "C2B280", "Eerie Black", "1B1B1B", "Eggplant", "614051", "Eggshell", "F0EAD6",
                "Egyptian Blue", "1034A6", "Electric Blue", "7DF9FF", "Electric Crimson", "FF003F",
                "Electric Cyan", "00FFFF", "Electric Green", "00FF00", "Electric Indigo", "6F00FF",
                "Electric Lavender", "F4BBFF", "Electric Lime", "CCFF00", "Electric Purple", "BF00FF",
                "Electric Ultramarine", "3F00FF", "Electric Violet", "8F00FF", "Electric Yellow", "FFFF33",
                "Emerald", "50C878", "Eminence", "6C3082", "English Green", "1B4D3E", "English Lavender", "B48395",
                "English Red", "AB4B52", "English Vermillion", "CC474B", "English Violet", "563C5C",
                "ETH Blue", "1F407A", "Eton Blue", "96C8A2", "Eucalyptus", "44D7A8", "Fallow", "C19A6B",
                "Falu Red", "801818", "Fandango", "B53389", "Fandango Pink", "DE5285", "Fashion Fuchsia", "F400A1",
                "Fawn", "E5AA70", "Feldgrau", "4D5D53", "Feldspar", "FDD5B1", "Fern Green", "4F7942",
                "Ferrari Red", "FF2800", "Field Drab", "6C541E", "Fiery Rose", "FF5470",
                "Fire Engine Red", "CE2029", "Flame", "E25822", "Flamingo Pink", "FC8EAC", "Flattery", "6B4423",
                "Flavescent", "F7E98E", "Flax", "EEDC82", "Flirt", "A2006D", "Fluorescent Orange", "FFBF00",
                "Fluorescent Pink", "FF1493", "Fluorescent Yellow", "CCFF00", "Folly", "FF004F",
                "French Beige", "A67B5B", "French Bistre", "856D4D", "French Blue", "0072BB",
                "French Fuchsia", "FD3F92", "French Lilac", "86608E", "French Lime", "9EFD38",
                "French Mauve", "D473D4", "French Pink", "FD6C9E", "French Plum", "811453",
                "French Puce", "4E1609", "French Raspberry", "C72C48", "French Rose", "F64A8A",
                "French Sky Blue", "77B5FE", "French Violet", "8806CE", "French Wine", "AC1E44",
                "Fresh Air", "A6E7FF", "Frostbite", "E936A7", "Fuchsia Pink", "FF77FF", "Fuchsia Purple", "CC397B",
                "Fuchsia Rose", "C74375", "Fulvous", "E48400", "Fuzzy Wuzzy", "CC6666", "Gamboge", "E49B0F",
                "Gamboge Orange", "996600", "Gargoyle Gas", "FFDF46", "Generic Viridian", "007F66",
                "Giant's Club", "B05C52", "Giants Orange", "FE5A1D", "Ginger", "B06500", "Glaucous", "6082B6",
                "Glitter", "E6E8FA", "Glossy Grape", "AB92B3", "GO Green", "00AB66", "Gold Fusion", "85754E",
                "Golden Brown", "996515", "Golden Poppy", "FCC200", "Golden Yellow", "FFDF00",
                "Granite Gray", "676767", "Granny Smith Apple", "A8E4A0", "Grape", "6F2DA8",
                "Gray-Asparagus", "465945", "Gray-Blue", "8C92AC", "Green-Blue", "1164B4", "Green-Cyan", "009966",
                "Green Lizard", "A7F432", "Green Sheen", "6EAEA1", "Grizzly", "885818", "Grullo", "A99A86",
                "Guppie Green", "00FF7F", "Gunmetal", "2a3439", "Halaya Ube", "663854", "Han Blue", "446CCF",
                "Han Purple", "5218FA", "Hansa Yellow", "E9D66B", "Harlequin", "3FFF00",
                "Harlequin Green", "46CB18", "Harvard Crimson", "C90016", "Harvest Gold", "DA9100",
                "Heart Gold", "808000", "Heat Wave", "FF7A00", "Heidelberg Gold", "CFB53B",
                "Heidelberg Red", "960018", "Heliotrope", "DF73FF", "Heliotrope Gray", "AA98A9",
                "Heliotrope Magenta", "AA00BB", "Hollywood Cerise", "F400A1", "Honolulu Blue", "006DB0",
                "Hooker's Green", "49796B", "Hot Magenta", "FF1DCE", "Hunter Green", "355E3B", "Iceberg", "71A6D2",
                "Icterine", "FCF75E", "Illuminating Emerald", "319177", "Imperial", "602F6B",
                "Imperial Blue", "002395", "Imperial Purple", "66023C", "Imperial Red", "ED2939",
                "Inchworm", "B2EC5D", "Independence", "4C516D", "India Green", "138808", "Indian Yellow", "E3A857",
                "Indigo Dye", "091F92", "International Klein Blue", "002FA7", "International Orange", "FF4F00",
                "Iris", "5A4FCF", "Irresistible", "B3446C", "Isabelline", "F4F0EC", "Islamic Green", "009000",
                "Italian Sky Blue", "B2FFFF", "Jade", "00A86B", "Japanese Carmine", "9D2933",
                "Japanese Indigo", "264348", "Japanese Violet", "5B3256", "Jasmine", "F8DE7E", "Jasper", "D73B3E",
                "Jazzberry Jam", "A50B5E", "Jelly Bean", "DA614E", "Jet", "343434", "Jonquil", "F4CA16",
                "Jordy Blue", "8AB9F1", "June Bud", "BDDA57", "Jungle Green", "29AB87", "Kelly Green", "4CBB17",
                "Kenyan Copper", "7C1C05", "Keppel", "3AB09E", "Key Lime", "E8F48C", "Kobe", "882D17",
                "Kobi", "E79FC4", "Kobicha", "6B4423", "Kombu Green", "354230", "KU Crimson", "E8000D",
                "La Salle Green", "087830", "Languid Lavender", "D6CADD", "Lapis Lazuli", "26619C",
                "Laser Lemon", "FFFF66", "Laurel Green", "A9BA9D", "Lava", "CF1020", "Lavender Blue", "CCCCFF",
                "Lavender Gray", "C4C3D0", "Lavender Indigo", "9457EB", "Lavender Magenta", "EE82EE",
                "Lavender Mist", "E6E6FA", "Lavender Pink", "FBAED2", "Lavender Purple", "967BB6",
                "Lavender Rose", "FBA0E3", "Leiden Blue", "001158", "Lemon", "FFF700", "Lemon Curry", "CCA01D",
                "Lemon Glacier", "FDFF00", "Lemon Lime", "E3FF00", "Lemon Meringue", "F6EABE",
                "Lemon Yellow", "FFF44F", "Lenurple", "BA93D8", "Licorice", "1A1110", "Liberty", "545AA7",
                "Light Apricot", "FDD5B1", "Light Brilliant Red", "FE2E2E", "Light Brown", "B5651D",
                "Light Carmine Pink", "E66771", "Light Cobalt Blue", "88ACE0", "Light Cornflower Blue", "93CCEA",
                "Light Crimson", "F56991", "Light Deep Pink", "FF5CCD", "Light French Beige", "C8AD7F",
                "Light Fuchsia Pink", "F984EF", "Light Grayish Magenta", "CC99CC", "Light Hot Pink", "FFB3DE",
                "Light Khaki", "F0E68C", "Light Medium Orchid", "D39BCB", "Light Moss Green", "ADDFAD",
                "Light Orchid", "E6A8D7", "Light Pastel Purple", "B19CD9", "Light Red Ochre", "E97451",
                "Light Salmon Pink", "FF9999", "Light Taupe", "B38B6D", "Light Thulian Pink", "E68FAC",
                "Lilac", "C8A2C8", "Lilac Luster", "AE98AA", "Limerick", "9DC209", "Lincoln Green", "195905",
                "Lion", "C19A6B", "Liseran Purple", "DE6FA1", "Little Boy Blue", "6CA0DC", "Liver", "674C47",
                "Liver Chestnut", "987456", "Livid", "6699CC", "Lumber", "FFE4CD", "Lust", "E62020",
                "Maastricht Blue", "001C3D", "Macaroni And Cheese", "FFBD88", "Magenta Haze", "9F4576",
                "Magenta-Pink", "CC338B", "Magic Mint", "AAF0D1", "Magic Potion", "FF4466", "Magnolia", "F8F4FF",
                "Mahogany", "C04000", "Maize", "FBEC5D", "Majorelle Blue", "6050DC", "Malachite", "0BDA51",
                "Manatee", "979AAA", "Mandarin", "F37A48", "Mango Tango", "FF8243", "Mantis", "74C365",
                "Mardi Gras", "880085", "Marigold", "EAA221", "Mauve", "E0B0FF", "Mauve Taupe", "915F6D",
                "Mauvelous", "EF98AA", "Maximum Blue", "47ABCC", "Maximum Yellow", "FAFA37", "May Green", "4C9141",
                "Maya Blue", "73C2FB", "Meat Brown", "E5B73B", "Medium Candy Apple Red", "E2062C",
                "Medium Carmine", "AF4035", "Medium Champagne", "F3E5AB", "Medium Electric Blue", "035096",
                "Medium Jungle Green", "1C352D", "Medium Lavender Magenta", "DDA0DD",
                "Medium Persian Blue", "0067A5", "Medium Red-Violet", "BB3385", "Medium Ruby", "AA4069",
                "Medium Sky Blue", "80DAEB", "Medium Spring Bud", "C9DC87", "Medium Taupe", "674C47",
                "Medium Tuscan Red", "79443B", "Medium Vermilion", "D9603B", "Mellow Apricot", "F8B878",
                "Mellow Yellow", "F8DE7E", "Melon", "FDBCB4", "Metallic Seaweed", "0A7E8C",
                "Metallic Sunburst", "9C7C38", "Metal Pink", "FF00FD", "Mexican Pink", "E4007C",
                "Midnight", "702670", "Midnight Green", "004953", "Mikado Yellow", "FFC40C", "Mindaro", "E3F988",
                "Ming", "36747D", "Minion Yellow", "F5E050", "Mint", "3EB489", "Mint Green", "98FF98",
                "Misty Moss", "BBB477", "Mode Beige", "967117", "Moonstone Blue", "73A9C2",
                "Mordant Red", "AE0C00", "Moss Green", "8A9A5B", "Mountain Meadow", "30BA8F",
                "Mountbatten Pink", "997A8D", "MSU Green", "18453B", "Mughal Green", "306030",
                "Mulberry", "C54B8C", "Mummy's Tomb", "828E84", "Mustard", "FFDB58", "Myrtle Green", "317873",
                "Mystic", "D65282", "Mystic Maroon", "AD4379", "Nadeshiko Pink", "F6ADC6",
                "Napier Green", "2A8000", "Naples Yellow", "FADA5E", "Navy Purple", "9457EB",
                "Neon Carrot", "FFA343", "Neon Fuchsia", "FE4164", "Neon Green", "39FF14", "New Car", "214FC6",
                "New York Pink", "D7837F", "Nickel", "727472", "Nijmegen Red", "BE311A",
                "Non-Photo Blue", "A4DDED", "North Texas Green", "059033", "Nyanza", "E9FFDB",
                "Ocean Blue", "4F42B5", "Ocean Boat Blue", "0077BE", "Ocean Green", "48BF91", "Ochre", "CC7722",
                "Office Green", "008000", "Ogre Odor", "FD5240", "Old Burgundy", "43302E", "Old Gold", "CFB53B",
                "Old Heliotrope", "563C5C", "Old Lavender", "796878", "Old Mauve", "673147",
                "Old Moss Green", "867E36", "Old Rose", "C08081", "Old Silver", "848482", "Olivine", "9AB973",
                "Onyx", "353839", "Opera Mauve", "B784A7", "Orange Peel", "FF9F00", "Orange Soda", "FA5B3D",
                "Orange-Yellow", "F8D568", "Orchid Pink", "F2BDCD", "Orioles Orange", "FB4F14",
                "Otter Brown", "654321", "Outer Space", "414A4C", "Outrageous Orange", "FF6E4A",
                "Oxford Blue", "002147", "OU Crimson Red", "990000", "Pacific Blue", "1CA9C9",
                "Pakistan Green", "006600", "Palatinate Blue", "273BE2", "Palatinate Purple", "682860",
                "Pale Aqua", "BCD4E6", "Pale Blue", "AFEEEE", "Pale Brown", "987654", "Pale Carmine", "AF4035",
                "Pale Cerulean", "9BC4E2", "Pale Chestnut", "DDADAF", "Pale Copper", "DA8A67",
                "Pale Cornflower Blue", "ABCDEF", "Pale Cyan", "87D3F8", "Pale Gold", "E6BE8A",
                "Pale Lavender", "DCD0FF", "Pale Magenta", "F984E5", "Pale Magenta-Pink", "FF99CC",
                "Pale Pink", "FADADD", "Pale Plum", "DDA0DD", "Pale Red-Violet", "DB7093",
                "Pale Robin Egg Blue", "96DED1", "Pale Silver", "C9C0BB", "Pale Spring Bud", "ECEBBD",
                "Pale Taupe", "BC987E", "Pale Violet", "CC99FF", "Pansy Purple", "78184A",
                "Paolo Veronese Green", "009B7D", "Paradise Pink", "E63E62", "Paris Green", "50C878",
                "Pastel Blue", "AEC6CF", "Pastel Brown", "836953", "Pastel Gray", "CFCFC4",
                "Pastel Green", "77DD77", "Pastel Magenta", "F49AC2", "Pastel Orange", "FFB347",
                "Pastel Pink", "DEA5A4", "Pastel Purple", "B39EB5", "Pastel Red", "FF6961",
                "Pastel Violet", "CB99C9", "Pastel Yellow", "FDFD96", "Patriarch", "800080",
                "Payne's Grey", "536878", "Peach", "FFE5B4", "Peach-Orange", "FFCC99", "Peach-Yellow", "FADFAD",
                "Pear", "D1E231", "Pearl", "EAE0C8", "Pearl Aqua", "88D8C0", "Pearly Purple", "B768A2",
                "Peridot", "E6E200", "Periwinkle", "CCCCFF", "Permanent Geranium Lake", "E12C2C",
                "Persian Blue", "1C39BB", "Persian Green", "00A693", "Persian Indigo", "32127A",
                "Persian Orange", "D99058", "Persian Pink", "F77FBE", "Persian Plum", "701C1C",
                "Persian Red", "CC3333", "Persian Rose", "FE28A2", "Persimmon", "EC5800", "Pewter Blue", "8BA8B7",
                "Phlox", "DF00FF", "Phthalo Blue", "000F89", "Phthalo Green", "123524", "Picton Blue", "45B1E8",
                "Pictorial Carmine", "C30B4E", "Piggy Pink", "FDDDE6", "Pine Green", "01796F",
                "Pineapple", "563C5C", "Pink Flamingo", "FC74FD", "Pink Lace", "FFDDF4", "Pink Lavender", "D8B2D1",
                "Pink-Orange", "FF9966", "Pink Pearl", "E7ACCF", "Pink Raspberry", "980036",
                "Pink Sherbet", "F78FA7", "Pistachio", "93C572", "Pixie Powder", "391285", "Platinum", "E5E4E2",
                "Plump Purple", "5946B2", "Polished Pine", "5DA493", "Pomp And Power", "86608E",
                "Popstar", "BE4F62", "Portland Orange", "FF5A36", "Princess Perfume", "FF85CF",
                "Princeton Orange", "F58025", "Prune", "701C1C", "Prussian Blue", "003153",
                "Psychedelic Purple", "DF00FF", "Puce", "CC8899", "Puce Red", "722F37", "Pullman Brown", "644117",
                "Pullman Green", "3B331C", "Pumpkin", "FF7518", "Purple Heart", "69359C",
                "Purple Mountain Majesty", "9678B6", "Purple Navy", "4E5180", "Purple Pizzazz", "FE4EDA",
                "Purple Plum", "9C51B6", "Purple Taupe", "50404D", "Purpureus", "9A4EAE", "Quartz", "51484F",
                "Queen Blue", "436B95", "Queen Pink", "E8CCD7", "Quick Silver", "A6A6A6",
                "Quinacridone Magenta", "8E3A59", "Rackley", "5D8AA8", "Radical Red", "FF355E",
                "Raisin Black", "242124", "Rajah", "FBAB60", "Raspberry", "E30B5D", "Raspberry Glace", "915F6D",
                "Raspberry Pink", "E25098", "Raspberry Rose", "B3446C", "Raw Sienna", "D68A59",
                "Raw Umber", "826644", "Razzle Dazzle Rose", "FF33CC", "Razzmatazz", "E3256B",
                "Razzmic Berry", "8D4E85", "Red-Brown", "A52A2A", "Red Devil", "860111", "Red-Orange", "FF5349",
                "Red-Purple", "E40078", "Red Salsa", "FD3A4A", "Red-Violet", "C71585", "Redwood", "A45A52",
                "Regalia", "522D80", "Registration Black", "000000", "Resolution Blue", "002387",
                "Rhythm", "777696", "Rich Black", "004040", "Rich Brilliant Lavender", "F1A7FE",
                "Rich Carmine", "D70040", "Rich Electric Blue", "0892D0", "Rich Lavender", "A76BCF",
                "Rich Lilac", "B666D2", "Rich Maroon", "B03060", "Rifle Green", "444C38", "Roast Coffee", "704241",
                "Robin Egg Blue", "00CCCC", "Rocket Metallic", "8A7F80", "Roman Silver", "838996",
                "Rose", "FF007F", "Rose Bonbon", "F9429E", "Rose Dust", "9E5E6F", "Rose Ebony", "674846",
                "Rose Gold", "B76E79", "Rose Madder", "E32636", "Rose Pink", "FF66CC", "Rose Quartz", "AA98A9",
                "Rose Red", "C21E56", "Rose Taupe", "905D5D", "Rose Vale", "AB4E52", "Rosewood", "65000B",
                "Rosso Corsa", "D40000", "Royal Azure", "0038A8", "Royal Fuchsia", "CA2C92",
                "Royal Purple", "7851A9", "Royal Yellow", "FADA5E", "Ruber", "CE4676", "Rubine Red", "D10056",
                "Ruby", "E0115F", "Ruby Red", "9B111E", "Ruddy", "FF0028", "Ruddy Brown", "BB6528",
                "Ruddy Pink", "E18E96", "Rufous", "A81C07", "Russet", "80461B", "Russian Green", "679267",
                "Russian Violet", "32174D", "Rust", "B7410E", "Rusty Red", "DA2C43",
                "Sacramento State Green", "00563F", "Safety Orange", "FF7800", "Safety Yellow", "EED202",
                "Saffron", "F4C430", "Sage", "BCB88A", "Saint Patrick's Blue", "23297A", "Salmon Pink", "FF91A4",
                "Sand", "C2B280", "Sand Dune", "967117", "Sandstorm", "ECD540", "Sandy Taupe", "967117",
                "Sangria", "92000A", "Sap Green", "507D2A", "Sapphire", "0F52BA", "Sapphire Blue", "0067A5",
                "Sasquatch Socks", "FF4681", "Satin Sheen Gold", "CBA135", "Scarlet", "FF2400",
                "Schauss Pink", "FF91AF", "School Bus Yellow", "FFD800", "Screamin' Green", "66FF66",
                "Sea Blue", "006994", "Sea Serpent", "4BC7CF", "Seal Brown", "59260B",
                "Selective Yellow", "FFBA00", "Sepia", "704214", "Shadow", "8A795D", "Shadow Blue", "778BA5",
                "Shampoo", "FFCFF1", "Shamrock Green", "009E60", "Sheen Green", "8FD400",
                "Shimmering Blush", "D98695", "Shiny Shamrock", "5FA778", "Shocking Pink", "FC0FC0",
                "Silver Chalice", "ACACAC", "Silver Lake Blue", "5D89BA", "Silver Pink", "C4AEAD",
                "Silver Sand", "BFC1C2", "Sinopia", "CB410B", "Sizzling Red", "FF3855",
                "Sizzling Sunrise", "FFDB00", "Skobeloff", "007474", "Sky Magenta", "CF71AF", "Smalt", "003399",
                "Slimy Green", "299617", "Smashed Pumpkin", "FF6D3A", "Smitten", "C84186", "Smoke", "738276",
                "Smokey Topaz", "832A0D", "Smoky Black", "100C08", "Smoky Topaz", "933D41", "Soap", "CEC8EF",
                "Solid Pink", "893843", "Sonic Silver", "757575", "Spartan Crimson", "9E1316",
                "Space Cadet", "1D2951", "Spanish Bistre", "807532", "Spanish Blue", "0070B8",
                "Spanish Carmine", "D10047", "Spanish Crimson", "E51A4C", "Spanish Gray", "989898",
                "Spanish Green", "009150", "Spanish Orange", "E86100", "Spanish Pink", "F7BFBE",
                "Spanish Red", "E60026", "Spanish Sky Blue", "00FFFF", "Spanish Violet", "4C2882",
                "Spanish Viridian", "007F5C", "Spicy Mix", "8B5f4D", "Spiro Disco Ball", "0FC0FC",
                "Spring Bud", "A7FC00", "Spring Frost", "87FF2A", "Star Command Blue", "007BB8",
                "Steel Pink", "CC33CC", "Steel Teal", "5F8A8B", "Stil De Grain Yellow", "FADA5E",
                "Stizza", "990000", "Stormcloud", "4F666A", "Straw", "E4D96F", "Strawberry", "FC5A8D",
                "Sugar Plum", "914E75", "Sunburnt Cyclops", "FF404C", "Sunglow", "FFCC33", "Sunny", "F2F27A",
                "Sunray", "E3AB57", "Sunset", "FAD6A5", "Sunset Orange", "FD5E53", "Super Pink", "CF6BA9",
                "Sweet Brown", "A83731", "Tangelo", "F94D00", "Tangerine", "F28500", "Tangerine Yellow", "FFCC00",
                "Tango Pink", "E4717A", "Tart Orange", "FB4D46", "Taupe", "483C32", "Taupe Gray", "8B8589",
                "Tea Green", "D0F0C0", "Tea Rose", "F88379", "Teal Blue", "367588", "Teal Deer", "99E6B3",
                "Teal Green", "00827F", "Telemagenta", "CF3476", "Tenne", "CD5700", "Terra Cotta", "E2725B",
                "Thulian Pink", "DE6FA1", "Tickle Me Pink", "FC89AC", "Tiffany Blue", "0ABAB5",
                "Tiger's Eye", "E08D3C", "Timberwolf", "DBD7D2", "Titanium Yellow", "EEE600", "Toolbox", "746CC0",
                "Topaz", "FFC87C", "Tractor Red", "FD0E35", "Trolley Grey", "808080",
                "Tropical Rain Forest", "00755E", "Tropical Violet", "CDA4DE", "True Blue", "0073CF",
                "Tufts Blue", "417DC1", "Tulip", "FF878D", "Tumbleweed", "DEAA88", "Turkish Rose", "B57281",
                "Turquoise Blue", "00FFEF", "Turquoise Green", "A0D6B4", "Turtle Green", "8A9A5B",
                "Tuscan", "FAD6A5", "Tuscan Brown", "6F4E37", "Tuscan Red", "7C4848", "Tuscan Tan", "A67B5B",
                "Tuscany", "C09999", "Twilight Lavender", "8A496B", "Tyrian Purple", "66023C", "UA Blue", "0033AA",
                "UA Red", "D9004C", "Ube", "8878C3", "UCLA Blue", "536895", "UCLA Gold", "FFB300",
                "UFO Green", "3CD070", "Ultramarine", "3F00FF", "Ultramarine Blue", "4166F5",
                "Ultra Pink", "FF6FFF", "Ultra Red", "FC6C85", "Umber", "635147", "Unbleached Silk", "FFDDCA",
                "United Nations Blue", "5B92E5", "University Of California Gold", "B78727",
                "Unmellow Yellow", "FFFF66", "UP Forest Green", "014421", "UP Maroon", "7B1113",
                "Upsdell Red", "AE2029", "Urobilin", "E1AD21", "USAFA Blue", "004F98", "USC Cardinal", "990000",
                "USC Gold", "FFCC00", "University Of Tennessee Orange", "F77F00", "Utah Crimson", "D3003F",
                "Utrecht Red", "F52A01", "Utrecht Yellow", "FFCD00", "Van Dyke Brown", "664228",
                "Vanilla", "F3E5AB", "Vanilla Ice", "F38FA9", "Vegas Gold", "C5B358", "Venetian Red", "C80815",
                "Verdigris", "43B3AE", "Vermilion", "E34234", "Veronica", "A020F0", "Very Light Azure", "74BBFB",
                "Very Light Blue", "6666FF", "Very Light Malachite Green", "64E986",
                "Very Light Tangelo", "FFB077", "Very Pale Orange", "FFDFBF", "Very Pale Yellow", "FFFFBF",
                "Violet-Blue", "324AB2", "Violet-Red", "F75394", "Viridian", "40826D", "Viridian Green", "009698",
                "Vista Blue", "7C9ED9", "Vivid Amber", "CC9900", "Vivid Auburn", "922724",
                "Vivid Burgundy", "9F1D35", "Vivid Cerise", "DA1D81", "Vivid Cerulean", "00AAEE",
                "Vivid Crimson", "CC0033", "Vivid Gamboge", "FF9900", "Vivid Lime Green", "A6D608",
                "Vivid Malachite", "00CC33", "Vivid Mulberry", "B80CE3", "Vivid Orange", "FF5F00",
                "Vivid Orange Peel", "FFA000", "Vivid Orchid", "CC00FF", "Vivid Raspberry", "FF006C",
                "Vivid Red", "F70D1A", "Vivid Red-Tangelo", "DF6124", "Vivid Sky Blue", "00CCFF",
                "Vivid Tangelo", "F07427", "Vivid Tangerine", "FFA089", "Vivid Vermilion", "E56024",
                "Vivid Violet", "9F00FF", "Vivid Yellow", "FFE302", "Volt", "CEFF00", "Wageningen Green", "34B233",
                "Warm Black", "004242", "Waterspout", "A4F4F9", "Weldon Blue", "7C98AB", "Wenge", "645452",
                "Wild Blue Yonder", "A2ADD0", "Wild Orchid", "D470A2", "Wild Strawberry", "FF43A4",
                "Wild Watermelon", "FC6C85", "Willpower Orange", "FD5800", "Windsor Tan", "A75502",
                "Wine", "722F37", "Wine Dregs", "673147", "Winter Sky", "FF007C", "Winter Wizard", "A0E6FF",
                "Wintergreen Dream", "56887D", "Wisteria", "C9A0DC", "Wood Brown", "C19A6B", "Xanadu", "738678",
                "Yale Blue", "0F4D92", "Yankees Blue", "1C2841", "Yellow Orange", "FFAE42",
                "Yellow Rose", "FFF000", "Yellow Sunshine", "FFF700", "Zaffre", "0014A8",
                "Zinnwaldite Brown", "2C1608", "Zomp", "39A78E");
            #endregion

            return ret;
        }

        static void Make(List<NamedColor> ret, params string[] values)
        {
            for (int i = 0; i < values.Length; i += 2)
            {
                ret.Add(new NamedColor(values[i], values[i + 1]));
            }
        }

        public static string WebColorToHex(string webColor)
        {
            if (s_Colors == null)
            {
                s_Colors = new Dictionary<string, string>();

                if (s_OtherColors != null)
                {
                    foreach (var kvp in s_OtherColors)
                    {
                        s_Colors.Add(kvp.Key, kvp.Value);
                    }
                }

                foreach (var cur in GetAllColors())
                {
                    string name = cur.Name;
                    string color = cur.HexValue;

                    name = name.Replace(" ", "");
                    name = name.Replace("-", "");
                    name = name.Replace("'", "");
                    name = name.ToLower();

#if DEBUG
                    if (Regex.IsMatch(name, "[^a-z]"))
                    {
                        throw new Exception();
                    }

                    if (Regex.IsMatch(name, "^([a-fA-F0-9][a-fA-F0-9][a-fA-F0-9]|[a-fA-F0-9][a-fA-F0-9][a-fA-F0-9][a-fA-F0-9][a-fA-F0-9][a-fA-F0-9])$"))
                    {
                        throw new Exception();
                    }

                    if (s_Colors.ContainsKey(name))
                    {
                        throw new Exception();
                    }
                    else
#endif
                    {
                        s_Colors.Add(name, color);
                    }
                }
            }

            webColor = webColor.ToLower().Replace(" ", "").Replace("-", "");

            if (s_Colors.ContainsKey(webColor))
            {
                return s_Colors[webColor];
            }

            return null;
        }

        public static ColorD FromWebColor(string webColor)
        {
            string hex = WebColorToHex(webColor);

            if (hex == null)
            {
                return new ColorD();
            }
            else
            {
                return ColorD.FromHex(hex);
            }
        }

        public static ColorD FromHex(string hex)
        {
            if (hex.Length == 3)
            {
                return new ColorD(
                    int.Parse(hex.Substring(0, 1), NumberStyles.HexNumber) / 15.0,
                    int.Parse(hex.Substring(1, 1), NumberStyles.HexNumber) / 15.0,
                    int.Parse(hex.Substring(2, 1), NumberStyles.HexNumber) / 15.0);
            }
            else
            {
                return new ColorD(
                    int.Parse(hex.Substring(0, 2), NumberStyles.HexNumber) / 255.0,
                    int.Parse(hex.Substring(2, 2), NumberStyles.HexNumber) / 255.0,
                    int.Parse(hex.Substring(4, 2), NumberStyles.HexNumber) / 255.0);
            }
        }

        public static ColorD ParseAny(string value)
        {
            if ((value.Length == 3 || value.Length == 6) &&
                Regex.IsMatch(value, "^[a-fA-F0-9]+$"))
            {
                return FromHex(value);
            }

            string hex = WebColorToHex(value);
            if (hex != null)
            {
                return FromHex(hex);
            }

            Match m = Regex.Match(value, "([0-9]+) *, *([0-9]+) *, *([0-9]+)");
            if (m.Success)
            {
                return new ColorD(
                    int.Parse(m.Groups[1].Value) / 255.0,
                    int.Parse(m.Groups[2].Value) / 255.0,
                    int.Parse(m.Groups[3].Value) / 255.0);
            }

            return new ColorD();
        }

        public string GetColorAsHex()
        {
            return
                Math.Max(0, Math.Min(255, (int)(R * 255.0))).ToString("X2") +
                Math.Max(0, Math.Min(255, (int)(G * 255.0))).ToString("X2") +
                Math.Max(0, Math.Min(255, (int)(B * 255.0))).ToString("X2");
        }

        public ColorD(double grayscale)
        {
            m_R = grayscale;
            m_G = grayscale;
            m_B = grayscale;
            m_A = 1.0;
        }

        public ColorD(double R, double G, double B)
        {
            m_R = R;
            m_G = G;
            m_B = B;
            m_A = 1.0;
        }

        public ColorD(double A, double R, double G, double B)
        {
            m_R = R;
            m_G = G;
            m_B = B;
            m_A = A;
        }

        public ColorD(double A, ColorD color)
        {
            m_R = color.R;
            m_G = color.G;
            m_B = color.B;
            m_A = A;
        }

        public static ColorD operator +(ColorD value1, ColorD value2)
        {
            return new ColorD(
                value1.A + value2.A,
                value1.R + value2.R,
                value1.G + value2.G,
                value1.B + value2.B);
        }

        public static ColorD operator -(ColorD value1, ColorD value2)
        {
            return new ColorD(
                value1.A - value2.A,
                value1.R - value2.R,
                value1.G - value2.G,
                value1.B - value2.B);
        }

        public static ColorD operator +(ColorD value1, double value2)
        {
            return new ColorD(
                value1.A,
                value1.R + value2,
                value1.G + value2,
                value1.B + value2);
        }

        public static ColorD operator -(ColorD value1, double value2)
        {
            return new ColorD(
                value1.A,
                value1.R - value2,
                value1.G - value2,
                value1.B - value2);
        }

        public static ColorD operator *(ColorD value1, ColorD value2)
        {
            return new ColorD(
                value1.A * value2.A,
                value1.R * value2.R,
                value1.G * value2.G,
                value1.B * value2.B);
        }

        public static ColorD operator /(ColorD value1, ColorD value2)
        {
            return new ColorD(
                value1.A / value2.A,
                value1.R / value2.R,
                value1.G / value2.G,
                value1.B / value2.B);
        }

        public static ColorD operator *(ColorD value1, double value2)
        {
            return new ColorD(
                value1.A,
                value1.R * value2,
                value1.G * value2,
                value1.B * value2);
        }

        public static ColorD operator /(ColorD value1, double value2)
        {
            return new ColorD(
                value1.A,
                value1.R / value2,
                value1.G / value2,
                value1.B / value2);
        }

        public static bool operator ==(ColorD value1, ColorD value2)
        {
            return
                (value1.R == value2.R) &&
                (value1.G == value2.G) &&
                (value1.B == value2.B) &&
                (value1.A == value2.A);
        }

        public override bool Equals(object obj)
        {
            if (obj is ColorD)
            {
                return ((ColorD)obj) == this;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return
                R.GetHashCode() ^
                G.GetHashCode() ^
                B.GetHashCode() ^
                A.GetHashCode();
        }

        public static bool operator !=(ColorD value1, ColorD value2)
        {
            return !(value1 == value2);
        }

#if !SILVERLIGHT
        public static implicit operator Color(ColorD from)
        {
            return Color.FromArgb(
                Math.Min(Math.Max((int)(from.m_A * 255.0), 0), 255),
                Math.Min(Math.Max((int)(from.m_R * 255.0), 0), 255),
                Math.Min(Math.Max((int)(from.m_G * 255.0), 0), 255),
                Math.Min(Math.Max((int)(from.m_B * 255.0), 0), 255));
        }

        public static implicit operator ColorD(Color from)
        {
            return new ColorD(
                from.A / 255.0,
                from.R / 255.0,
                from.G / 255.0,
                from.B / 255.0);
        }
#endif

        public ColorD AlphaBlend(ColorD back)
        {
            return new ColorD(back.A,
                m_R * (m_A) + back.R * (1.0 - m_A),
                m_G * (m_A) + back.G * (1.0 - m_A),
                m_B * (m_A) + back.B * (1.0 - m_A));
        }

        public static implicit operator ColorHSB(ColorD from)
        {
            return ColorHSB.FromColorD(from);
        }

        public static implicit operator ColorHSL(ColorD from)
        {
            return ColorHSL.FromColorD(from);
        }

        public static implicit operator ColorCMYK(ColorD from)
        {
            return ColorCMYK.FromColorD(from);
        }

        public static implicit operator ColorXY(ColorD from)
        {
            return ColorXY.FromColorD(from);
        }

        public static implicit operator ColorYUV(ColorD from)
        {
            return ColorYUV.FromColorD(from);
        }

        public static ColorD FromGray(double grayscale)
        {
            return new ColorD(grayscale);
        }

        public static ColorD FromRGB(double r, double g, double b)
        {
            return new ColorD(r, g, b);
        }

        public static ColorD FromRGB(double a, double r, double g, double b)
        {
            return new ColorD(a, r, g, b);
        }

        public ColorD FromGamma(double gamma)
        {
            return new ColorD(A,
                Math.Pow(R, gamma),
                Math.Pow(G, gamma),
                Math.Pow(B, gamma));
        }

        public ColorD ToGamma(double gamma)
        {
            gamma = 1.0 / gamma;
            return new ColorD(A,
                Math.Pow(R, gamma),
                Math.Pow(G, gamma),
                Math.Pow(B, gamma));
        }
    }

    [DebuggerDisplay("\\{AHSB = ({A}, {H}, {S}, {B})\\}")]
    public struct ColorHSB
    {
        double m_H;
        double m_S;
        double m_B;
        double m_A;

        public override string ToString()
        {
            return string.Format(
                "ColorHSB [A={0}, H={1}, S={2}, B={3}]",
                A, H, S, B);
        }

        /// <summary>
        /// Hue of color.  0.0 to 360.0.  0 is Red, 120 is Green, 240 is blue.
        /// </summary>
        public double H
        {
            get
            {
                return m_H;
            }
        }

        /// <summary>
        /// Saturation of color.  0.0 to 1.0.  1.0 is fully saturated, 0.0 is grey/white.
        /// </summary>
        public double S
        {
            get
            {
                return m_S;
            }
        }

        /// <summary>
        /// Brightness of color.  0.0 to 1.0.
        /// </summary>
        public double B
        {
            get
            {
                return m_B;
            }
        }

        /// <summary>
        /// Alpha component of color.  0.0 to 1.0.  0.0 is transparent, 1.0 is opaque.
        /// </summary>
        public double A
        {
            get
            {
                return m_A;
            }
        }

        public ColorHSB(double grayscale)
        {
            m_H = grayscale;
            m_S = grayscale;
            m_B = grayscale;
            m_A = 1.0;
        }

        public ColorHSB(double H, double S, double B)
        {
            m_H = H;
            m_S = S;
            m_B = B;
            m_A = 1.0;
        }

        public ColorHSB(double A, double H, double S, double B)
        {
            m_H = H;
            m_S = S;
            m_B = B;
            m_A = A;
        }

        public ColorHSB(double A, ColorHSB color)
        {
            m_H = color.H;
            m_S = color.S;
            m_B = color.B;
            m_A = A;
        }

        public static ColorHSB operator +(ColorHSB value1, ColorHSB value2)
        {
            return new ColorHSB(
                value1.A + value2.A,
                value1.H + value2.H,
                value1.S + value2.S,
                value1.B + value2.B);
        }

        public static ColorHSB operator -(ColorHSB value1, ColorHSB value2)
        {
            return new ColorHSB(
                value1.A - value2.A,
                value1.H - value2.H,
                value1.S - value2.S,
                value1.B - value2.B);
        }

        public static ColorHSB operator +(ColorHSB value1, double value2)
        {
            return new ColorHSB(
                value1.A,
                value1.H + value2,
                value1.S + value2,
                value1.B + value2);
        }

        public static ColorHSB operator -(ColorHSB value1, double value2)
        {
            return new ColorHSB(
                value1.A,
                value1.H - value2,
                value1.S - value2,
                value1.B - value2);
        }

        public static ColorHSB operator *(ColorHSB value1, ColorHSB value2)
        {
            return new ColorHSB(
                value1.A * value2.A,
                value1.H * value2.H,
                value1.S * value2.S,
                value1.B * value2.B);
        }

        public static ColorHSB operator /(ColorHSB value1, ColorHSB value2)
        {
            return new ColorHSB(
                value1.A / value2.A,
                value1.H / value2.H,
                value1.S / value2.S,
                value1.B / value2.B);
        }

        public static ColorHSB operator *(ColorHSB value1, double value2)
        {
            return new ColorHSB(
                value1.A,
                value1.H * value2,
                value1.S * value2,
                value1.B * value2);
        }

        public static ColorHSB operator /(ColorHSB value1, double value2)
        {
            return new ColorHSB(
                value1.A,
                value1.H / value2,
                value1.S / value2,
                value1.B / value2);
        }

        public static bool operator ==(ColorHSB value1, ColorHSB value2)
        {
            return
                (value1.H == value2.H) &&
                (value1.S == value2.S) &&
                (value1.B == value2.B) &&
                (value1.A == value2.A);
        }

        public override bool Equals(object obj)
        {
            if (obj is ColorHSB)
            {
                return ((ColorHSB)obj) == this;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return
                H.GetHashCode() ^
                S.GetHashCode() ^
                B.GetHashCode() ^
                A.GetHashCode();
        }

        public static bool operator !=(ColorHSB value1, ColorHSB value2)
        {
            return !(value1 == value2);
        }

        public static implicit operator ColorHSB(ColorHSL from)
        {
            return (ColorHSB)(ColorD)from;
        }

        public static implicit operator ColorHSB(ColorCMYK from)
        {
            return (ColorHSB)(ColorD)from;
        }

        public static implicit operator ColorHSB(ColorYUV from)
        {
            return (ColorHSB)(ColorD)from;
        }

        public static implicit operator ColorHSL(ColorHSB from)
        {
            return (ColorHSL)(ColorD)from;
        }

        public static implicit operator ColorYUV(ColorHSB from)
        {
            return (ColorYUV)(ColorD)from;
        }

        public static ColorHSB FromHSB(double h, double s, double b)
        {
            return new ColorHSB(h, s, b);
        }

        public static ColorHSB FromHSB(double a, double h, double s, double b)
        {
            return new ColorHSB(a, h, s, b);
        }

        public static implicit operator ColorD(ColorHSB from)
        {
            double r = 0;
            double g = 0;
            double b = 0;

            if (from.S == 0)
            {
                return new ColorD(from.A, from.B, from.B, from.B);
            }
            else
            {
                double sectorPos = from.H / 60.0;
                int sectorNumber = (int)(Math.Floor(sectorPos));

                double fractionalSector = sectorPos - sectorNumber;

                double p = from.B * (1.0 - from.S);
                double q = from.B * (1.0 - (from.S * fractionalSector));
                double t = from.B * (1.0 - (from.S * (1 - fractionalSector)));

                switch (sectorNumber)
                {
                    case 0:
                        r = from.B;
                        g = t;
                        b = p;
                        break;
                    case 1:
                        r = q;
                        g = from.B;
                        b = p;
                        break;
                    case 2:
                        r = p;
                        g = from.B;
                        b = t;
                        break;
                    case 3:
                        r = p;
                        g = q;
                        b = from.B;
                        break;
                    case 4:
                        r = t;
                        g = p;
                        b = from.B;
                        break;
                    case 5:
                        r = from.B;
                        g = p;
                        b = q;
                        break;
                }
            }

            return new ColorD(from.A, r, g, b);
        }

        public static ColorHSB FromColorD(ColorD from)
        {
            double max = Math.Max(from.R, Math.Max(from.G, from.B));
            double min = Math.Min(from.R, Math.Min(from.G, from.B));

            double h = 0.0;
            if (max == from.R && from.G >= from.B)
            {
                h = 60 * (from.G - from.B) / (max - min);
            }
            else if (max == from.R && from.G < from.B)
            {
                h = 60 * (from.G - from.B) / (max - min) + 360;
            }
            else if (max == from.G)
            {
                h = 60 * (from.B - from.R) / (max - min) + 120;
            }
            else if (max == from.B)
            {
                h = 60 * (from.R - from.G) / (max - min) + 240;
            }

            double s = (max == 0) ? 0.0 : (1.0 - (min / max));

            return new ColorHSB(from.A, h, s, (double)max);
        }

#if !SILVERLIGHT
        public static implicit operator Color(ColorHSB from)
        {
            return (ColorD)from;
        }

        public static implicit operator ColorHSB(Color from)
        {
            return (ColorD)from;
        }
#endif
    }

    [DebuggerDisplay("\\{AHSL = ({A}, {H}, {S}, {L})\\}")]
    public struct ColorHSL
    {
        double m_H;
        double m_S;
        double m_L;
        double m_A;

        public override string ToString()
        {
            return string.Format(
                "ColorHSL [A={0}, H={1}, S={2}, L={3}]",
                A, H, S, L);
        }

        /// <summary>
        /// Hue of color.  0.0 to 360.0.  0 is Red, 120 is Green, 240 is blue.
        /// </summary>
        public double H
        {
            get
            {
                return m_H;
            }
        }

        /// <summary>
        /// Saturation of color.  0.0 to 1.0.  1.0 is fully saturated, 0.0 is grey/white.
        /// </summary>
        public double S
        {
            get
            {
                return m_S;
            }
        }

        /// <summary>
        /// Lightness of color.  0.0 to 1.0.  0.5 is the color.
        /// </summary>
        public double L
        {
            get
            {
                return m_L;
            }
        }

        /// <summary>
        /// Alpha component of color.  0.0 to 1.0.  0.0 is transparent, 1.0 is opaque.
        /// </summary>
        public double A
        {
            get
            {
                return m_A;
            }
        }

        public ColorHSL(double grayscale)
        {
            m_H = grayscale;
            m_S = grayscale;
            m_L = grayscale;
            m_A = 1.0;
        }

        public ColorHSL(double H, double S, double L)
        {
            m_H = H;
            m_S = S;
            m_L = L;
            m_A = 1.0;
        }

        public ColorHSL(double A, double H, double S, double L)
        {
            m_H = H;
            m_S = S;
            m_L = L;
            m_A = A;
        }

        public ColorHSL(double A, ColorHSL color)
        {
            m_H = color.H;
            m_S = color.S;
            m_L = color.L;
            m_A = A;
        }

        public static ColorHSL operator +(ColorHSL value1, ColorHSL value2)
        {
            return new ColorHSL(
                value1.A + value2.A,
                value1.H + value2.H,
                value1.S + value2.S,
                value1.L + value2.L);
        }

        public static ColorHSL operator -(ColorHSL value1, ColorHSL value2)
        {
            return new ColorHSL(
                value1.A - value2.A,
                value1.H - value2.H,
                value1.S - value2.S,
                value1.L - value2.L);
        }

        public static ColorHSL operator +(ColorHSL value1, double value2)
        {
            return new ColorHSL(
                value1.A,
                value1.H + value2,
                value1.S + value2,
                value1.L + value2);
        }

        public static ColorHSL operator -(ColorHSL value1, double value2)
        {
            return new ColorHSL(
                value1.A,
                value1.H - value2,
                value1.S - value2,
                value1.L - value2);
        }

        public static ColorHSL operator *(ColorHSL value1, ColorHSL value2)
        {
            return new ColorHSL(
                value1.A * value2.A,
                value1.H * value2.H,
                value1.S * value2.S,
                value1.L * value2.L);
        }

        public static ColorHSL operator /(ColorHSL value1, ColorHSL value2)
        {
            return new ColorHSL(
                value1.A / value2.A,
                value1.H / value2.H,
                value1.S / value2.S,
                value1.L / value2.L);
        }

        public static ColorHSL operator *(ColorHSL value1, double value2)
        {
            return new ColorHSL(
                value1.A,
                value1.H * value2,
                value1.S * value2,
                value1.L * value2);
        }

        public static ColorHSL operator /(ColorHSL value1, double value2)
        {
            return new ColorHSL(
                value1.A,
                value1.H / value2,
                value1.S / value2,
                value1.L / value2);
        }

        public static bool operator ==(ColorHSL value1, ColorHSL value2)
        {
            return
                (value1.H == value2.H) &&
                (value1.S == value2.S) &&
                (value1.L == value2.L) &&
                (value1.A == value2.A);
        }

        public override bool Equals(object obj)
        {
            if (obj is ColorHSL)
            {
                return ((ColorHSL)obj) == this;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return
                H.GetHashCode() ^
                S.GetHashCode() ^
                L.GetHashCode() ^
                A.GetHashCode();
        }

        public static bool operator !=(ColorHSL value1, ColorHSL value2)
        {
            return !(value1 == value2);
        }

        public static implicit operator ColorHSL(ColorCMYK from)
        {
            return (ColorHSL)(ColorD)from;
        }

        public static implicit operator ColorHSL(ColorYUV from)
        {
            return (ColorHSL)(ColorD)from;
        }

        public static implicit operator ColorD(ColorHSL from)
        {
            if (from.S == 0)
            {
                return new ColorD(from.A, from.L, from.L, from.L);
            }
            else
            {
                double q = (from.L < 0.5) ? (from.L * (1.0 + from.S)) : (from.L + from.S - (from.L * from.S));
                double p = (2.0 * from.L) - q;

                double Hk = from.H / 360.0;
                double[] T = new double[3];
                T[0] = Hk + (1.0 / 3.0);
                T[1] = Hk;
                T[2] = Hk - (1.0 / 3.0);

                for (int i = 0; i < 3; i++)
                {
                    if (T[i] < 0) T[i] += 1.0;
                    if (T[i] > 1) T[i] -= 1.0;

                    if ((T[i] * 6) < 1)
                    {
                        T[i] = p + ((q - p) * 6.0 * T[i]);
                    }
                    else if ((T[i] * 2.0) < 1)
                    {
                        T[i] = q;
                    }
                    else if ((T[i] * 3.0) < 2)
                    {
                        T[i] = p + (q - p) * ((2.0 / 3.0) - T[i]) * 6.0;
                    }
                    else T[i] = p;
                }

                return new ColorD(from.A, T[0], T[1], T[2]);
            }
        }

        public static implicit operator ColorCMYK(ColorHSL from)
        {
            return (ColorCMYK)(ColorD)from;
        }

        public static implicit operator ColorYUV(ColorHSL from)
        {
            return (ColorYUV)(ColorD)from;
        }

        public static ColorHSL FromHSL(double h, double s, double l)
        {
            return new ColorHSL(h, s, l);
        }

        public static ColorHSL FromHSL(double a, double h, double s, double l)
        {
            return new ColorHSL(a, h, s, l);
        }

        public static ColorHSL FromColorD(ColorD from)
        {
            double h = 0, s = 0, l = 0;

            double max = Math.Max(from.R, Math.Max(from.G, from.B));
            double min = Math.Min(from.R, Math.Min(from.G, from.B));

            if (max == min)
            {
                h = 0;
            }
            else if (max == from.R && from.G >= from.B)
            {
                h = 60.0 * (from.G - from.B) / (max - min);
            }
            else if (max == from.R && from.G < from.B)
            {
                h = 60.0 * (from.G - from.B) / (max - min) + 360.0;
            }
            else if (max == from.G)
            {
                h = 60.0 * (from.B - from.R) / (max - min) + 120.0;
            }
            else if (max == from.B)
            {
                h = 60.0 * (from.R - from.G) / (max - min) + 240.0;
            }

            l = (max + min) / 2.0;

            if (l == 0 || max == min)
            {
                s = 0;
            }
            else if (0 < l && l <= 0.5)
            {
                s = (max - min) / (max + min);
            }
            else if (l > 0.5)
            {
                s = (max - min) / (2 - (max + min));
            }

            return new ColorHSL(from.A, h, s, l);
        }

#if !SILVERLIGHT
        public static implicit operator Color(ColorHSL from)
        {
            return (ColorD)from;
        }

        public static implicit operator ColorHSL(Color from)
        {
            return (ColorD)from;
        }
#endif
    }

    [DebuggerDisplay("\\{AYUV = ({A}, {Y}, {U}, {V})\\}")]
    public struct ColorYUV
    {
        double m_Y;
        double m_U;
        double m_V;
        double m_A;

        public override string ToString()
        {
            return string.Format(
                "ColorYUV [A={0}, Y={1}, U={2}, V={3}]",
                A, Y, U, V);
        }

        /// <summary>
        /// The luma value.  0.0 to 1.0.  0.5 is the color.
        /// </summary>
        public double Y
        {
            get
            {
                return m_Y;
            }
        }

        /// <summary>
        /// The first chrominance value.  0.0 to 1.0.
        /// </summary>
        public double U
        {
            get
            {
                return m_U;
            }
        }

        /// <summary>
        /// The second chrominance value.  0.0 to 1.0.
        /// </summary>
        public double V
        {
            get
            {
                return m_V;
            }
        }

        /// <summary>
        /// Alpha component of color.  0.0 to 1.0.  0.0 is transparent, 1.0 is opaque.
        /// </summary>
        public double A
        {
            get
            {
                return m_A;
            }
        }

        public ColorYUV(double grayscale)
        {
            m_Y = grayscale;
            m_U = grayscale;
            m_V = grayscale;
            m_A = 1.0;
        }

        public ColorYUV(double Y, double U, double V)
        {
            m_Y = Y;
            m_U = U;
            m_V = V;
            m_A = 1.0;
        }

        public ColorYUV(double A, double Y, double U, double V)
        {
            m_Y = Y;
            m_U = U;
            m_V = V;
            m_A = A;
        }

        public ColorYUV(double A, ColorYUV color)
        {
            m_Y = color.Y;
            m_U = color.U;
            m_V = color.V;
            m_A = A;
        }

        public static ColorYUV operator +(ColorYUV value1, ColorYUV value2)
        {
            return new ColorYUV(
                value1.A + value2.A,
                value1.Y + value2.Y,
                value1.U + value2.U,
                value1.V + value2.V);
        }

        public static ColorYUV operator -(ColorYUV value1, ColorYUV value2)
        {
            return new ColorYUV(
                value1.A - value2.A,
                value1.Y - value2.Y,
                value1.U - value2.U,
                value1.V - value2.V);
        }

        public static ColorYUV operator +(ColorYUV value1, double value2)
        {
            return new ColorYUV(
                value1.A,
                value1.Y + value2,
                value1.U + value2,
                value1.V + value2);
        }

        public static ColorYUV operator -(ColorYUV value1, double value2)
        {
            return new ColorYUV(
                value1.A,
                value1.Y - value2,
                value1.U - value2,
                value1.V - value2);
        }

        public static ColorYUV operator *(ColorYUV value1, ColorYUV value2)
        {
            return new ColorYUV(
                value1.A * value2.A,
                value1.Y * value2.Y,
                value1.U * value2.U,
                value1.V * value2.V);
        }

        public static ColorYUV operator /(ColorYUV value1, ColorYUV value2)
        {
            return new ColorYUV(
                value1.A / value2.A,
                value1.Y / value2.Y,
                value1.U / value2.U,
                value1.V / value2.V);
        }

        public static ColorYUV operator *(ColorYUV value1, double value2)
        {
            return new ColorYUV(
                value1.A,
                value1.Y * value2,
                value1.U * value2,
                value1.V * value2);
        }

        public static ColorYUV operator /(ColorYUV value1, double value2)
        {
            return new ColorYUV(
                value1.A,
                value1.Y / value2,
                value1.U / value2,
                value1.V / value2);
        }

        public static bool operator ==(ColorYUV value1, ColorYUV value2)
        {
            return
                (value1.Y == value2.Y) &&
                (value1.U == value2.U) &&
                (value1.V == value2.V) &&
                (value1.A == value2.A);
        }

        public override bool Equals(object obj)
        {
            if (obj is ColorYUV)
            {
                return ((ColorYUV)obj) == this;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return
                Y.GetHashCode() ^
                U.GetHashCode() ^
                V.GetHashCode() ^
                A.GetHashCode();
        }

        public static bool operator !=(ColorYUV value1, ColorYUV value2)
        {
            return !(value1 == value2);
        }

        public static implicit operator ColorYUV(ColorCMYK from)
        {
            return (ColorYUV)(ColorD)from;
        }

        public static implicit operator ColorCMYK(ColorYUV from)
        {
            return (ColorCMYK)(ColorD)from;
        }

        public static ColorYUV FromYUV(double y, double u, double v)
        {
            return new ColorYUV(y, u, v);
        }

        public static ColorYUV FromYUV(double a, double y, double u, double v)
        {
            return new ColorYUV(a, y, u, v);
        }

        public static ColorYUV FromColorD(ColorD from)
        {
            return new ColorYUV(from.A,
                0.299 * from.R + 0.587 * from.G + 0.114 * from.B,
                -0.14713 * from.R - 0.28886 * from.G + 0.436 * from.B,
                0.615 * from.R - 0.51499 * from.G - 0.10001 * from.B);
        }

        public static implicit operator ColorD(ColorYUV from)
        {
            return new ColorD(
                from.A,
                from.Y + 1.139837398373983740 * from.V,
                from.Y - 0.3946517043589703515 * from.U - 0.5805986066674976801 * from.V,
                from.Y + 2.032110091743119266 * from.U);
        }

#if !SILVERLIGHT
        public static implicit operator Color(ColorYUV from)
        {
            return (ColorD)from;
        }

        public static implicit operator ColorYUV(Color from)
        {
            return (ColorD)from;
        }
#endif
    }

    [DebuggerDisplay("\\{ACMYK = ({A}, {C}, {M}, {Y}, {K})\\}")]
    public struct ColorCMYK
    {
        double m_C;
        double m_M;
        double m_Y;
        double m_K;
        double m_A;

        public override string ToString()
        {
            return string.Format(
                "ColorCMYK [A={0}, C={1}, M={2}, Y={3}, K={4}]",
                A, C, M, Y, K);
        }

        /// <summary>
        /// The cyan value.  0.0 to 1.0.
        /// </summary>
        public double C
        {
            get
            {
                return m_C;
            }
        }

        /// <summary>
        /// The magenta value.  0.0 to 1.0.
        /// </summary>
        public double M
        {
            get
            {
                return m_M;
            }
        }

        /// <summary>
        /// The yellow value.  0.0 to 1.0.
        /// </summary>
        public double Y
        {
            get
            {
                return m_Y;
            }
        }

        /// <summary>
        /// The key value.  0.0 to 1.0.  0.0 is the color, 1.0 is black.
        /// </summary>
        public double K
        {
            get
            {
                return m_K;
            }
        }

        /// <summary>
        /// Alpha component of color.  0.0 to 1.0.  0.0 is transparent, 1.0 is opaque.
        /// </summary>
        public double A
        {
            get
            {
                return m_A;
            }
        }

        public ColorCMYK(double C, double M, double Y, double K)
        {
            m_C = C;
            m_M = M;
            m_Y = Y;
            m_K = K;
            m_A = 1.0;
        }

        public ColorCMYK(double A, double C, double M, double Y, double K)
        {
            m_C = C;
            m_M = M;
            m_Y = Y;
            m_K = K;
            m_A = A;
        }

        public ColorCMYK(double A, ColorCMYK color)
        {
            m_C = color.C;
            m_M = color.M;
            m_Y = color.Y;
            m_K = color.K;
            m_A = A;
        }

        public static ColorCMYK operator +(ColorCMYK value1, ColorCMYK value2)
        {
            return new ColorCMYK(
                value1.A + value2.A,
                value1.C + value2.C,
                value1.M + value2.M,
                value1.Y + value2.Y,
                value1.K + value2.K);
        }

        public static ColorCMYK operator -(ColorCMYK value1, ColorCMYK value2)
        {
            return new ColorCMYK(
                value1.A - value2.A,
                value1.C - value2.C,
                value1.M - value2.M,
                value1.Y - value2.Y,
                value1.K - value2.K);
        }

        public static ColorCMYK operator +(ColorCMYK value1, double value2)
        {
            return new ColorCMYK(
                value1.A,
                value1.C + value2,
                value1.M + value2,
                value1.Y + value2,
                value1.K + value2);
        }

        public static ColorCMYK operator -(ColorCMYK value1, double value2)
        {
            return new ColorCMYK(
                value1.A,
                value1.C - value2,
                value1.M - value2,
                value1.Y - value2,
                value1.K - value2);
        }

        public static ColorCMYK operator *(ColorCMYK value1, ColorCMYK value2)
        {
            return new ColorCMYK(
                value1.A * value2.A,
                value1.C * value2.C,
                value1.M * value2.M,
                value1.Y * value2.Y,
                value1.K * value2.K);
        }

        public static ColorCMYK operator /(ColorCMYK value1, ColorCMYK value2)
        {
            return new ColorCMYK(
                value1.A / value2.A,
                value1.C / value2.C,
                value1.M / value2.M,
                value1.Y / value2.Y,
                value1.K / value2.K);
        }

        public static ColorCMYK operator *(ColorCMYK value1, double value2)
        {
            return new ColorCMYK(
                value1.A,
                value1.C * value2,
                value1.M * value2,
                value1.Y * value2,
                value1.K * value2);
        }

        public static ColorCMYK operator /(ColorCMYK value1, double value2)
        {
            return new ColorCMYK(
                value1.A,
                value1.C / value2,
                value1.M / value2,
                value1.Y / value2,
                value1.K / value2);
        }

        public static bool operator ==(ColorCMYK value1, ColorCMYK value2)
        {
            return
                (value1.C == value2.C) &&
                (value1.M == value2.M) &&
                (value1.Y == value2.Y) &&
                (value1.K == value2.K) &&
                (value1.A == value2.A);
        }

        public override bool Equals(object obj)
        {
            if (obj is ColorCMYK)
            {
                return ((ColorCMYK)obj) == this;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return
                C.GetHashCode() ^
                M.GetHashCode() ^
                Y.GetHashCode() ^
                K.GetHashCode() ^
                A.GetHashCode();
        }

        public static bool operator !=(ColorCMYK value1, ColorCMYK value2)
        {
            return !(value1 == value2);
        }

        public static implicit operator ColorCMYK(ColorHSB from)
        {
            return (ColorCMYK)(ColorD)from;
        }

        public static implicit operator ColorD(ColorCMYK from)
        {
            return new ColorD(from.A,
                (1 - from.C) * (1 - from.K),
                (1 - from.M) * (1 - from.K),
                (1 - from.Y) * (1 - from.K));
        }

        public static ColorCMYK FromCMYK(double c, double m, double y, double k)
        {
            return new ColorCMYK(c, m, y, k);
        }

        public static ColorCMYK FromCMYK(double a, double c, double m, double y, double k)
        {
            return new ColorCMYK(a, c, m, y, k);
        }

        public static ColorCMYK FromColorD(ColorD from)
        {
            double c = 1.0 - from.R;
            double m = 1.0 - from.G;
            double y = 1.0 - from.B;

            double k = (double)Math.Min(c, Math.Min(m, y));

            if (k == 1.0)
            {
                return new ColorCMYK(from.A, 0, 0, 0, 1);
            }
            else
            {
                return new ColorCMYK(from.A, (c - k) / (1 - k), (m - k) / (1 - k), (y - k) / (1 - k), k);
            }
        }

#if !SILVERLIGHT
        public static implicit operator Color(ColorCMYK from)
        {
            return (ColorD)from;
        }

        public static implicit operator ColorCMYK(Color from)
        {
            return (ColorD)from;
        }
#endif
    }

    [DebuggerDisplay("\\{AXY = ({A}, {X}, {Y})\\}")]
    public struct ColorXY
    {
        double m_X;
        double m_Y;
        double m_A;

        public override string ToString()
        {
            return string.Format(
                "ColorXY [A={0}, X={1}, Y={2}]",
                A, X, Y);
        }

        /// <summary>
        /// The X value of the color.  0.0 to 1.0.
        /// </summary>
        public double X
        {
            get
            {
                return m_X;
            }
        }

        /// <summary>
        /// The Y value of the color.  0.0 to 1.0.
        /// </summary>
        public double Y
        {
            get
            {
                return m_Y;
            }
        }

        public double A
        {
            get
            {
                return m_A;
            }
        }

        public ColorXY(double X, double Y)
        {
            m_X = X;
            m_Y = Y;
            m_A = 1.0;
        }

        public ColorXY(double A, double X, double Y)
        {
            m_X = X;
            m_Y = Y;
            m_A = A;
        }

        public ColorXY(double A, ColorXY color)
        {
            m_X = color.Y;
            m_Y = color.Y;
            m_A = A;
        }

        public static ColorXY operator +(ColorXY value1, ColorXY value2)
        {
            return new ColorXY(
                value1.A + value2.A,
                value1.X + value2.X,
                value1.Y + value2.Y);
        }

        public static ColorXY operator -(ColorXY value1, ColorXY value2)
        {
            return new ColorXY(
                value1.A - value2.A,
                value1.X + value2.X,
                value1.Y + value2.Y);
        }

        public static ColorXY operator +(ColorXY value1, double value2)
        {
            return new ColorXY(
                value1.A,
                value1.X + value2,
                value1.Y + value2);
        }

        public static ColorXY operator -(ColorXY value1, double value2)
        {
            return new ColorXY(
                value1.A,
                value1.X - value2,
                value1.Y - value2);
        }

        public static ColorXY operator *(ColorXY value1, ColorXY value2)
        {
            return new ColorXY(
                value1.A * value2.A,
                value1.X * value2.X,
                value1.Y * value2.Y);
        }

        public static ColorXY operator /(ColorXY value1, ColorXY value2)
        {
            return new ColorXY(
                value1.A / value2.A,
                value1.X / value2.X,
                value1.Y / value2.Y);
        }

        public static ColorXY operator *(ColorXY value1, double value2)
        {
            return new ColorXY(
                value1.A,
                value1.X * value2,
                value1.Y * value2);
        }

        public static ColorXY operator /(ColorXY value1, double value2)
        {
            return new ColorXY(
                value1.A,
                value1.X / value2,
                value1.Y / value2);
        }

        public static bool operator ==(ColorXY value1, ColorXY value2)
        {
            return
                (value1.X == value2.X) &&
                (value1.Y == value2.Y) &&
                (value1.A == value2.A);
        }

        public override bool Equals(object obj)
        {
            if (obj is ColorXY)
            {
                return ((ColorXY)obj) == this;
            }
            else
            {
                return base.Equals(obj);
            }
        }

        public override int GetHashCode()
        {
            return
                X.GetHashCode() ^
                Y.GetHashCode() ^
                A.GetHashCode();
        }

        public static bool operator !=(ColorXY value1, ColorXY value2)
        {
            return !(value1 == value2);
        }

        public static implicit operator ColorD(ColorXY from)
        {
            ColorD temp = XYToColor(new PointD(from.X, from.Y));
            return new ColorD(from.A, temp.R, temp.G, temp.B);
        }

        public static ColorXY FromColorD(ColorD from)
        {
            PointD pt = ColorToXY(from);
            return new ColorXY(from.A, pt.X, pt.Y);
        }

        public static ColorXY FromXY(double x, double y)
        {
            return new ColorXY(x, y);
        }

        public static ColorXY FromXY(double a, double x, double y)
        {
            return new ColorXY(a, x, y);
        }

#if !SILVERLIGHT
        public static implicit operator Color(ColorXY from)
        {
            return (ColorD)from;
        }

        public static implicit operator ColorXY(Color from)
        {
            return (ColorD)from;
        }
#endif

        static ColorD XYToColor(PointD xy)
        {
            PointD ptR = new PointD(1.0, 0.0);
            PointD ptG = new PointD(0.0, 1.0);
            PointD ptB = new PointD(0.0, 0.0);

            if (!IsInReach(xy, ptR, ptG, ptB))
            {
                PointD pAB = PointD.ClosestPoint(ptR, ptG, xy);
                PointD pAC = PointD.ClosestPoint(ptB, ptR, xy);
                PointD pBC = PointD.ClosestPoint(ptG, ptB, xy);

                double dAB = PointD.Distance(xy, pAB);
                double dAC = PointD.Distance(xy, pAC);
                double dBC = PointD.Distance(xy, pBC);

                double lowest = dAB;
                PointD closestPoint = pAB;

                if (dAC < lowest)
                {
                    lowest = dAC;
                    closestPoint = pAC;
                }

                if (dBC < lowest)
                {
                    lowest = dBC;
                    closestPoint = pBC;
                }

                xy.X = closestPoint.X;
                xy.Y = closestPoint.Y;
            }

            double x = xy.X;
            double y = xy.Y;
            double z = 1.0 - x - y;

            double y2 = 1.0;
            double x2 = y2 / y * x;
            double z2 = y2 / y * z;

            double r = x2 * 1.656492 - y2 * 0.354851 - z2 * 0.255038;
            double g = (-x2) * 0.707196 + y2 * 1.655397 + z2 * 0.036152;
            double b = x2 * 0.051713 - y2 * 0.121364 + z2 * 1.01153;

            if (r > b && r > g && r > 1.0)
            {
                g /= r;
                b /= r;
                r = 1.0;
            }
            else if (g > b && g > r && g > 1.0)
            {
                r /= g;
                b /= g;
                g = 1.0;
            }
            else if (b > r && b > g && b > 1.0)
            {
                r /= b;
                g /= b;
                b = 1.0;
            }

            r = r <= 0.0031308 ? 12.92 * r : 1.055 * Math.Pow(r, 0.4166666567325592) - 0.055;
            g = g <= 0.0031308 ? 12.92 * g : 1.055 * Math.Pow(g, 0.4166666567325592) - 0.055;

            double f = b = b <= 0.0031308 ? 12.92 * b : 1.055 * Math.Pow(b, 0.4166666567325592) - 0.055;

            if (r > b && r > g)
            {
                if (r > 1.0)
                {
                    g /= r;
                    b /= r;
                    r = 1.0;
                }
            }
            else if (g > b && g > r)
            {
                if (g > 1.0)
                {
                    r /= g;
                    b /= g;
                    g = 1.0;
                }
            }
            else if (b > r && b > g && b > 1.0)
            {
                r /= b;
                g /= b;
                b = 1.0;
            }

            if (r < 0.0)
            {
                r = 0.0;
            }

            if (g < 0.0)
            {
                g = 0.0;
            }

            if (b < 0.0)
            {
                b = 0.0;
            }

            return new ColorD(r, g, b);
        }

        static PointD ColorToXY(ColorD color)
        {
            PointD ptR = new PointD(1.0, 0.0);
            PointD ptG = new PointD(0.0, 1.0);
            PointD ptB = new PointD(0.0, 0.0);

            double r = color.R > 0.04045 ? Math.Pow((color.R + 0.055) / 1.055, 2.4000000953674316) : color.R / 12.92;
            double g = color.G > 0.04045 ? Math.Pow((color.G + 0.055) / 1.055, 2.4000000953674316) : color.G / 12.92;
            double b = color.B > 0.04045 ? Math.Pow((color.B + 0.055) / 1.055, 2.4000000953674316) : color.B / 12.92;

            double x = r * 0.664511 + g * 0.154324 + b * 0.162028;
            double y = r * 0.283881 + g * 0.668433 + b * 0.047685;
            double z = r * 0.000088 + g * 0.0723 + b * 0.986039;

            PointD xy = new PointD(x / (x + y + z), y / (x + y + z));

            if (double.IsNaN(xy.X))
            {
                xy.X = 0.0;
            }

            if (double.IsNaN(xy.Y))
            {
                xy.Y = 0.0;
            }

            if (!IsInReach(xy, ptR, ptG, ptB))
            {
                PointD pAB = PointD.ClosestPoint(ptR, ptG, xy);
                PointD pAC = PointD.ClosestPoint(ptB, ptR, xy);
                PointD pBC = PointD.ClosestPoint(ptG, ptB, xy);

                double dAB = PointD.Distance(xy, pAB);
                double dAC = PointD.Distance(xy, pAC);
                double dBC = PointD.Distance(xy, pBC);

                double lowest = dAB;
                PointD closestPoint = pAB;

                if (dAC < lowest)
                {
                    lowest = dAC;
                    closestPoint = pAC;
                }

                if (dBC < lowest)
                {
                    lowest = dBC;
                    closestPoint = pBC;
                }

                xy.X = closestPoint.X;
                xy.Y = closestPoint.Y;
            }

            return new PointD(xy.X, xy.Y);
        }

        public bool IsInReach()
        {
            PointD ptR = new PointD(1.0, 0.0);
            PointD ptG = new PointD(0.0, 1.0);
            PointD ptB = new PointD(0.0, 0.0);

            return IsInReach(new PointD(X, Y), ptR, ptG, ptB);
        }

        static bool IsInReach(PointD point, PointD ptR, PointD ptG, PointD ptB)
        {
            PointD v1 = new PointD(ptG.X - ptR.X, ptG.Y - ptR.Y);
            PointD v2 = new PointD(ptB.X - ptR.X, ptB.Y - ptR.Y);
            PointD q = new PointD(point.X - ptR.X, point.Y - ptR.Y);

            double s = PointD.CrossProduct(q, v2) / PointD.CrossProduct(v1, v2);
            double t = PointD.CrossProduct(v1, q) / PointD.CrossProduct(v1, v2);

            if (s >= 0.0 && t >= 0.0 && s + t <= 1.0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        class PointD
        {
            public double X { get; set; }
            public double Y { get; set; }

            public PointD(double X, double Y)
            {
                this.X = X;
                this.Y = Y;
            }

            public static double Distance(PointD a, PointD b)
            {
                double dx = a.X - b.X;
                double dy = a.Y - b.Y;

                return Math.Sqrt(dx * dx + dy * dy);
            }

            public static double CrossProduct(PointD a, PointD b)
            {
                return a.X * b.Y - a.Y * b.X;
            }

            public static PointD ClosestPoint(PointD a, PointD b, PointD c)
            {
                PointD ptAP = new PointD(c.X - a.X, c.Y - a.Y);
                PointD ptAB = new PointD(b.X - a.X, b.Y - a.Y);

                double apAb = ptAP.X * ptAB.X + ptAP.Y * ptAB.Y;
                double ab2 = ptAB.X * ptAB.X + ptAB.Y * ptAB.Y;
                double t = apAb / ab2;

                if (t < 0.0)
                {
                    t = 0.0;
                }
                else if (t > 1.0)
                {
                    t = 1.0;
                }

                return new PointD(a.X + ptAB.X * t, a.Y + ptAB.Y * t);
            }
        }
    }

    [DebuggerDisplay("\\{Name} = #{Color}")]
    public class NamedColor
    {
        public string Name { get; private set; }
        public string HexValue { get; private set; }

        public NamedColor(string Name, string HexValue)
        {
            this.Name = Name;
            this.HexValue = HexValue;
        }
    }
}
