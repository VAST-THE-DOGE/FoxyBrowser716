public struct Theme
{
	public required Color PrimaryBackgroundColor;
	public Color PrimaryBackgroundColorSlightTransparent
		=> Color.FromArgb(200, PrimaryBackgroundColor.R, PrimaryBackgroundColor.G, PrimaryBackgroundColor.B);
	public Color PrimaryBackgroundColorVeryTransparent
		=> Color.FromArgb(100, PrimaryBackgroundColor.R, PrimaryBackgroundColor.G, PrimaryBackgroundColor.B);

	public required Color SecondaryBackgroundColor;
	public Color SecondaryBackgroundColorSlightTransparent
		=> Color.FromArgb(200, SecondaryBackgroundColor.R, SecondaryBackgroundColor.G, SecondaryBackgroundColor.B);
	public Color SecondaryBackgroundColorVeryTransparent
		=> Color.FromArgb(100, SecondaryBackgroundColor.R, SecondaryBackgroundColor.G, SecondaryBackgroundColor.B);

	public required Color PrimaryForegroundColor;
	public Color PrimaryForegroundColorSlightTransparent
		=> Color.FromArgb(200, PrimaryForegroundColor.R, PrimaryForegroundColor.G, PrimaryForegroundColor.B);
	public Color PrimaryForegroundColorVeryTransparent
		=> Color.FromArgb(100, PrimaryForegroundColor.R, PrimaryForegroundColor.G, PrimaryForegroundColor.B);

	public required Color SecondaryForegroundColor;
	public Color SecondaryForegroundColorSlightTransparent
		=> Color.FromArgb(200, SecondaryForegroundColor.R, SecondaryForegroundColor.G, SecondaryForegroundColor.B);
	public Color SecondaryForegroundColorVeryTransparent
		=> Color.FromArgb(100, SecondaryForegroundColor.R, SecondaryForegroundColor.G, SecondaryForegroundColor.B);

	public required Color PrimaryAccentColor;
	public Color PrimaryAccentColorSlightTransparent
		=> Color.FromArgb(200, PrimaryAccentColor.R, PrimaryAccentColor.G, PrimaryAccentColor.B);
	public Color PrimaryAccentColorVeryTransparent
		=> Color.FromArgb(100, PrimaryAccentColor.R, PrimaryAccentColor.G, PrimaryAccentColor.B);

	public required Color SecondaryAccentColor;
	public Color SecondaryAccentColorSlightTransparent
		=> Color.FromArgb(200, SecondaryAccentColor.R, SecondaryAccentColor.G, SecondaryAccentColor.B);
	public Color SecondaryAccentColorVeryTransparent
		=> Color.FromArgb(100, SecondaryAccentColor.R, SecondaryAccentColor.G, SecondaryAccentColor.B);

	public required Color PrimaryHighlightColor;
	public Color PrimaryHighlightColorSlightTransparent
		=> Color.FromArgb(200, PrimaryHighlightColor.R, PrimaryHighlightColor.G, PrimaryHighlightColor.B);
	public Color PrimaryHighlightColorVeryTransparent
		=> Color.FromArgb(100, PrimaryHighlightColor.R, PrimaryHighlightColor.G, PrimaryHighlightColor.B);

	public required Color SecondaryHighlightColor;
	public Color SecondaryHighlightColorSlightTransparent
		=> Color.FromArgb(200, SecondaryHighlightColor.R, SecondaryHighlightColor.G, SecondaryHighlightColor.B);
	public Color SecondaryHighlightColorVeryTransparent
		=> Color.FromArgb(100, SecondaryHighlightColor.R, SecondaryHighlightColor.G, SecondaryHighlightColor.B);

	public required Color YesColor;
	public Color YesColorSlightTransparent
		=> Color.FromArgb(200, YesColor.R, YesColor.G, YesColor.B);
	public Color YesColorVeryTransparent
		=> Color.FromArgb(100, YesColor.R, YesColor.G, YesColor.B);

	public required Color NoColor;
	public Color NoColorSlightTransparent
		=> Color.FromArgb(200, NoColor.R, NoColor.G, NoColor.B);
	public Color NoColorVeryTransparent
		=> Color.FromArgb(100, NoColor.R, NoColor.G, NoColor.B);
}
