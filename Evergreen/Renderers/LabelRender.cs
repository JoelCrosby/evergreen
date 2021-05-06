using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Gtk;

using LibGit2Sharp;

using Pango;

namespace Evergreen.Renderers
{
	public static class LabelRenderer
	{
		private const int Margin = 2;
		private const int Padding = 6;

		private static string LabelText(Reference r)
		{
			var escaped = GLib.Markup.EscapeText(r.CanonicalName);
			return $"<span size='smaller'>{escaped}</span>";
		}

		private static int GetLabelWidth(Pango.Layout layout, Reference r)
		{
			var smaller = LabelText(r);

            layout.SetMarkup(smaller);
            layout.GetPixelSize(out var w, out var _);

			return w + (Padding * 2);
		}

		public static int Width(Widget  widget, FontDescription font, List<Reference> labels)
		{
			if (labels == null)
			{
				return 0;
			}

			var ret = 0;

			var ctx = widget.PangoContext;
            var layout = new Pango.Layout(ctx)
            {
                FontDescription = font,
            };

            foreach (var r in labels)
			{
				ret += GetLabelWidth(layout, r) + Margin;
			}

			return ret + Margin;
		}

		private static string ClassFromRef(Reference reference)
		{
            if (reference.IsLocalBranch)
            {
                return "branch";
            }

            if (reference.IsRemoteTrackingBranch)
            {
                return "remote";
            }

            if (reference.IsTag)
            {
                return "tag";
            }

            if (reference.IsNote)
            {
                return "stash";
            }

            return null;
        }

		private static int RenderLabel(
            Widget widget, Cairo.Context cr, Pango.Layout  layout,
            Reference r, double x, double y, int height)
		{
			var context = widget.StyleContext;
			var smaller = LabelText(r);

			layout.SetMarkup(smaller);
            layout.GetPixelSize(out var w, out var h);

            context.Save();

			var style_class = ClassFromRef(r);

			if (style_class != null)
			{
				context.AddClass(style_class);
			}

			var rtl = (widget.StyleContext.State & StateFlags.DirRtl) != 0;

			if (rtl)
			{
				x -= w + (Padding * 2);
			}

			context.RenderBackground(
                cr,
                x,
                y + Margin,
                w + (Padding * 2),
                height - (Margin * 2)
            );

			context.RenderFrame(
                cr,
			    x,
			    y + Margin,
			    w + (Padding * 2),
			    height - (Margin * 2)
            );

			context.RenderLayout(
                cr,
			    x + Padding,
			    y + ((height - h) / 2.0) - 1,
			    layout
            );

			context.Restore();
			return w;
		}

		public static void Draw(
            Widget widget, FontDescription font, Cairo.Context context,
            List<Reference> labels, Gdk.Rectangle area)
		{
			double pos;

			var rtl = (widget.StyleContext.State & StateFlags.DirRtl) != 0;

			if (!rtl)
			{
				pos = area.X + Margin + 0.5;
			}
			else
			{
				pos = area.X + area.Width - Margin - 0.5;
			}

			context.Save();
			context.LineWidth = 1.0;

			var ctx = widget.PangoContext;
            var layout = new Pango.Layout(ctx)
            {
                FontDescription = font,
            };

            foreach (var r in labels)
			{
				var w = RenderLabel(
                    widget,
                    context,
				    layout,
				    r,
				    (int)pos,
				    area.Y,
				    area.Height
                );

				var o = w + (Padding * 2) + Margin;
				pos += rtl ? -o : o;
			}

			context.Restore();
		}

		public static Reference GetRefAtPos(
            Widget widget, FontDescription font, List<Reference> labels,
            int x, out int hotX)
		{
			hotX = 0;

			if (labels == null)
			{
				return null;
			}

			var ctx = widget.PangoContext;
            var layout = new Pango.Layout(ctx)
            {
                FontDescription = font,
            };

            var start = Margin;
			Reference ret = null;

			foreach (var r in labels)
			{
				var width = GetLabelWidth(layout, r);

				if (x >= start && x <= start + width)
				{
					ret = r;
					hotX = x - start;

					break;
				}

				start += width + Margin;
			}

			return ret;
		}

		private static byte ConvertColorChannel(byte color, byte alpha)
		{
			return (byte)(alpha != 0 ? color / (alpha / 255.0) : 0);
		}

		private static void ConvertBgraToRgba(IReadOnlyList<byte> src, IList<byte> dst, int width, int height)
		{
			var i = 0;

			for (var y = 0; y < height; ++y)
			{
				for (var x = 0; x < width; ++x)
				{
					dst[i] = ConvertColorChannel(src[i + 2], src[i + 3]);
					dst[i + 1] = ConvertColorChannel(src[i + 1], src[i + 3]);
					dst[i + 2] = ConvertColorChannel(src[i], src[i + 3]);
					dst[i + 3] = src[i + 3];

					i += 4;
				}
			}
		}

		public static Gdk.Pixbuf RenderRef(
            Widget  widget, FontDescription font, Reference  r, int height, int minwidth)
		{
			var ctx = widget.PangoContext;
            var layout = new Pango.Layout(ctx)
            {
                FontDescription = font,
            };

            var width = Math.Max(GetLabelWidth(layout, r), minwidth);

			var surface = new Cairo.ImageSurface(
                Cairo.Format.ARGB32,
			    width + 2,
			    height + 2
            );

            var context = new Cairo.Context(surface)
            {
                LineWidth = 1,
            };

            RenderLabel(widget, context, layout, r, 1, 1, height);
			var data = surface.Data;

			var ret = new Gdk.Pixbuf(
                Gdk.Colorspace.Rgb,
                true,
                8,
                width + 2,
                height + 2
            );

			var pixData = new[] { Marshal.ReadByte(ret.Pixels) };

			ConvertBgraToRgba(data, pixData, width + 2, height + 2);

			return ret;
		}
	}
}
