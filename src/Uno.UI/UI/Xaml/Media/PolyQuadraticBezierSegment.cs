namespace Windows.UI.Xaml.Media
{
	public partial class PolyQuadraticBezierSegment : PathSegment
	{
		public PolyQuadraticBezierSegment()
		{
			Points = new PointCollection();
		}

		#region Points

		public PointCollection Points
		{
			get => (PointCollection)this.GetValue(PointsProperty);
			set => this.SetValue(PointsProperty, value);
		}

		public static DependencyProperty PointsProperty { get; } =
			DependencyProperty.Register(
				nameof(Points),
				typeof(PointCollection),
				typeof(PolyQuadraticBezierSegment),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: OnPointsChanged,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure
				)
			);

		private static void OnPointsChanged(DependencyObject dependencyobject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyobject is PolyQuadraticBezierSegment segment)
			{
				if (args.OldValue is PointCollection oldCollection)
				{
					oldCollection.UnRegisterChangedListener(segment.OnPointsChanged);
				}
				if (args.NewValue is PointCollection newCollection)
				{
					newCollection.RegisterChangedListener(segment.OnPointsChanged);
				}
			}
		}

		private void OnPointsChanged() => this.InvalidateMeasure();

		#endregion
	}
}
