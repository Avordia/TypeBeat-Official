// Create a new file: TypeBeat.Game/ui/BeatReactiveSprite.cs
using System;
using System.Linq;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;

namespace TypeBeat.Game.ui
{
    public partial class BeatReactiveSprite : Container
    {
        private readonly Sprite sprite;
        private ITrack track;
        private Track concreteTrack;
        private readonly float[] amplitude_buffer = new float[32];
        private int amplitude_buffer_index;
        private float initialScale;
        
        /// <summary>
        /// The maximum scale as a percentage of the base size.
        /// 1.0 = no change, 1.5 = 50% larger, etc.
        /// </summary>
        public float MaxScalePercentage { get; set; } = 1.18f;

        public BeatReactiveSprite(Sprite sprite)
        {
            this.sprite = sprite;
            AutoSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            
            Child = sprite;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            initialScale = Scale.X;  // Now we capture the scale after it's properly set
        }

        public void SetTrack(ITrack newTrack)
        {
            track = newTrack;
            concreteTrack = newTrack as Track;
            Array.Clear(amplitude_buffer, 0, amplitude_buffer.Length);
        }

        protected override void Update()
        {
            base.Update();

            if (concreteTrack == null || !concreteTrack.IsRunning)
                return;

            var frequency_data = concreteTrack.CurrentAmplitudes.FrequencyAmplitudes;
            var frequency_span = frequency_data.Span;

            if (frequency_span.Length == 0)
                return;

            int startIndex = frequency_span.Length / 20;  // Start at 5%
            int endIndex = frequency_span.Length / 5;     // End at 20%
            int rangeLength = endIndex - startIndex;
            
            // Get the average amplitude of the selected frequency range
            float current_amplitude = 0;
            for (int i = startIndex; i < endIndex; i++)
            {
                current_amplitude += frequency_span[i];
            }
            current_amplitude /= rangeLength;
            amplitude_buffer[amplitude_buffer_index] = current_amplitude;
            amplitude_buffer_index = (amplitude_buffer_index + 1) % amplitude_buffer.Length;
            
            var average_amplitude = amplitude_buffer.Average();

            var base_scale = initialScale;
            var max_scale = initialScale * MaxScalePercentage;
            const float bounce_threshold = 1.85f;  // Less sensitive to reduce shakiness
            const float decay_speed = 170;    // Slower decay for smoother movement
            
            var amplitude_ratio = current_amplitude / Math.Max(average_amplitude, 0.001f);
            
            if (amplitude_ratio > bounce_threshold)
            {
                float bounce_scale = base_scale + (amplitude_ratio - bounce_threshold) * 0.3f;
                bounce_scale = Math.Min(bounce_scale, max_scale);
                
                this.ScaleTo(bounce_scale, 50, Easing.OutExpo)
                    .Then()
                    .ScaleTo(base_scale, decay_speed, Easing.OutBounce);
            }
        }
    }
}