using System.Windows.Input;

namespace FoxyBrowser716.HomeWidgets;

internal class RoguelikePlayer
{
	public double top;
	public double left;
	public Key? lastUpDown;
	public Key? lastLeftRight;
	public Dictionary<RoguelikeCards.CardId, int> Cards = [];
	public double CurrentHealth { get; set; }
	public double MaxHealth { get; set; }
	public double OriginalMaxHealth { get; set; }
}