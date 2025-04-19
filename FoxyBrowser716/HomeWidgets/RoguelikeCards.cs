namespace FoxyBrowser716.HomeWidgets;

public class RoguelikeCards
{
	public enum CardId
	{
		// common
		smallMovementSpeed,
		
		// uncommon
		
		
		// rare
		
		
		// epic
		
		
		// legendary
		
		
	}
	
	public static readonly IReadOnlyDictionary<int, RoguelikeCards> Cards = 
		new Dictionary<int, RoguelikeCards>
		{
			//TODO: refine these ideas
			/*[0]   = new("Health","", CardRarity.Common),
			[1]   = new("Speed","", CardRarity.Common),
			[2]   = new("Small Damage","", CardRarity.Common),
			[3]   = new("Small Fire Rate","", CardRarity.Common),
			[4]   = new("Small Bullet Speed","", CardRarity.Common),
			[5]   = new("Small Reload Time","", CardRarity.Common),
			[6]   = new("Small Shield","", CardRarity.Common),
			[7]   = new("Small Damage Reduction","", CardRarity.Common),
			[8]   = new("Small Extra Money","", CardRarity.Common),
			[9]   = new("Stonks","turns saved money into more money over time", CardRarity.Common),
			[10]  = new("Lucky","", CardRarity.Uncommon),
			[11]  = new("Medium Damage","", CardRarity.Uncommon),
			[12]  = new("Coin Magnet","", CardRarity.Uncommon),
			[13]  = new("Small Regen","", CardRarity.Uncommon),
			[14]  = new("Small Knockback","", CardRarity.Uncommon),
			[15]  = new("Medium Fire Rate","", CardRarity.Uncommon),
			[16]  = new("Medium Bullet Speed","", CardRarity.Uncommon),
			[17]  = new("Medium Reload Time","", CardRarity.Uncommon),
			[18]  = new("Homing Ammo","", CardRarity.Uncommon),
			[19]  = new("Heavy Shield","", CardRarity.Uncommon),
			[20]  = new("Ammo on Kill","", CardRarity.Rare),
			[21]  = new("Multishot","", CardRarity.Rare),
			[22]  = new("Piercing","", CardRarity.Rare),
			[23]  = new("Heavy Damage","", CardRarity.Rare),
			[24]  = new("Medium Regen","", CardRarity.Rare),
			[25]  = new("Medium Knockback","", CardRarity.Rare),
			[26]  = new("Heavy Fire Rate","", CardRarity.Rare),
			[27]  = new("Heavy Bullet Speed","", CardRarity.Rare),
			[28]  = new("Small Bullet Size","", CardRarity.Rare),
			[29]  = new("Heavy Reload Time","", CardRarity.Rare),
			[30]  = new("4-leaf Clover","", CardRarity.Epic),
			[31]  = new("Extreme Damage","", CardRarity.Epic),
			[32]  = new("Heavy Regen","", CardRarity.Epic),
			[33]  = new("Heavy Knockback","", CardRarity.Epic),
			[34]  = new("Extreme Fire Rate","", CardRarity.Epic),
			[35]  = new("Explosive Blast","", CardRarity.Epic),
			[36]  = new("Large Bullet Size","", CardRarity.Epic),
			[37]  = new("Poison Shot","", CardRarity.Epic),
			[38]  = new("Fire Shot","burns an enemy for a few seconds, spreading to nearby enemies", CardRarity.Epic),
			[39]  = new("Max Health on Kill","", CardRarity.Epic),
			[40]  = new("Wealth Is Power","", CardRarity.Epic),
			[41]  = new("Heavy Damage Reduction","", CardRarity.Epic),
			[42]  = new("Teleporter (turn into puddle and go to cursor)","", CardRarity.Legendary),
			[43]  = new("Unreal Damage","", CardRarity.Legendary),
			[44]  = new("Vampire","", CardRarity.Legendary),
			[45]  = new("Minigun","", CardRarity.Legendary),
			[46]  = new("Extra Life (AA Battery)","", CardRarity.Legendary),
			[47]  = new("Singularity","", CardRarity.Legendary),
			[48]  = new("Ice Shot","", CardRarity.Legendary),
			[49]  = new("Mine Layer","spawn mines where you walk", CardRarity.Legendary),
			[50]  = new("Elemental Mastery","", CardRarity.Legendary),
			[51]  = new("Shrapnel","bullets explode into a cluster of smaller less powerful bullets (can stack for extra explosions)", CardRarity.Legendary),*/
		}.AsReadOnly();

	public readonly string Title;
	public readonly string Description;
	public readonly CardRarity Rarity;
	
	private RoguelikeCards(string title, string description, CardRarity rarity)
	{
		Title = title;
		Description = description;
		Rarity = rarity;
	}

	public enum CardRarity
	{
		Common,
		Uncommon,
		Rare,
		Epic,
		Legendary,
	}
}