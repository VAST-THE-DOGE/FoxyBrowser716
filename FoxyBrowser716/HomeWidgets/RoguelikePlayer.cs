using System.Windows.Input;

namespace FoxyBrowser716.HomeWidgets;

public class RoguelikePlayer
{
	public double top;
	public double left;

	public Key? lastUpDown;
	public Key? lastLeftRight;

	public Dictionary<RoguelikeCards.CardId, int> Cards = [];
}