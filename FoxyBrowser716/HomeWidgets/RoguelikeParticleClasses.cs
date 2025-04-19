using System.Drawing;
using System.Numerics;
using System.Windows.Media;
using System.Windows.Shapes;

namespace FoxyBrowser716.HomeWidgets;

public static class RoguelikeParticleClasses
{
	public class Explosion
	{
		private List<BoxVectorParticle> particles = [];
		
		//TODO
		
	}
	
	public class BoxVectorParticle
	{
		public Vector2 Velocity;
		public Vector2 Acceleration;
		public int ParticleSize;
		public Line Box;

		public void Move()
		{
			Box.X1 += Velocity.X;
			Box.X2 += Velocity.X;
			Box.Y1 += Velocity.Y;
			Box.Y2 += Velocity.Y;
			
			Velocity.X += Acceleration.X;
			Velocity.Y += Acceleration.Y;
		}

		public BoxVectorParticle(Vector2 velocity, Vector2 acceleration, Point start, int size, Brush brush)
		{
			Velocity = velocity;
			Acceleration = acceleration;
			ParticleSize = size;
			Box = new Line
			{
				X1 = start.X,
				X2 = start.X,
				Y1 = start.Y,
				Y2 = start.Y + size,

				StrokeThickness = size,
				Stroke = brush,
			};
		}
	}
}