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
		private const int margin = 2;
		private const int padding = 6;

		private static string label_text(Reference r)
		{
			var escaped = GLib.Markup.EscapeText(r.CanonicalName);
			return $"<span size='smaller'>{escaped}</span>";
		}

		private static int GetLabelWidth(Pango.Layout layout, Reference r)
		{
			var smaller = label_text(r);

            layout.SetMarkup(smaller);
            layout.GetPixelSize(out var w, out var _);

			return w + (padding * 2);
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
                FontDescription = font
            };

            foreach (var r in labels)
			{
				ret += GetLabelWidth(layout, r) + margin;
			}

			return ret + margin;
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
			var smaller = label_text(r);

			layout.SetMarkup(smaller);
            layout.GetPixelSize(out var w, out var h);

            context.Save();

			var style_class = ClassFromRef(r);

			if (style_class != null)
			{
				context.AddClass(style_class);
			}

			var rtl = (widget.StyleContext.State & Gtk.StateFlags.DirRtl) != 0;

			if (rtl)
			{
				x -= w + (padding * 2);
			}

			context.RenderBackground(
                cr,
                x,
                y + margin,
                w + (padding * 2),
                height - (margin * 2)
            );

			context.RenderFrame(
                cr,
			    x,
			    y + margin,
			    w + (padding * 2),
			    height - (margin * 2)
            );

			context.RenderLayout(
                cr,
			    x + padding,
			    y + ((height - h) / 2.0) - 1,
			    layout
            );

			context.Restore();
			return w;
		}

		public static void draw(
            Widget widget, FontDescription font, Cairo.Context context,
            List<Reference> labels, Gdk.Rectangle area)
		{
			double pos;

			var rtl = (widget.StyleContext.State & Gtk.StateFlags.DirRtl) != 0;

			if (!rtl)
			{
				pos = area.X + margin + 0.5;
			}
			else
			{
				pos = area.X + area.Width - margin - 0.5;
			}

			context.Save();
			context.LineWidth = 1.0;

			var ctx = widget.PangoContext;
            var layout = new Pango.Layout(ctx)
            {
                FontDescription = font
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

				var o = w + (padding * 2) + margin;
				pos += rtl ? -o : o;
			}

			context.Restore();
		}

		public static Reference GetRefAtPos(
            Widget widget, FontDescription font, List<Reference> labels,
            int x, out int hot_x)
		{
			hot_x = 0;

			if (labels == null)
			{
				return null;
			}

			var ctx = widget.PangoContext;
            var layout = new Pango.Layout(ctx)
            {
                FontDescription = font
            };

            var start = margin;
			Reference ret = null;

			foreach (var r in labels)
			{
				var width = GetLabelWidth(layout, r);

				if (x >= start && x <= start + width)
				{
					ret = r;
					hot_x = x - start;

					break;
				}

				start += width + margin;
			}

			return ret;
		}

		private static byte ConvertColorChannel(byte color, byte alpha)
		{
			return (byte)((alpha != 0) ? (color / (alpha / 255.0)) : 0);
		}

		private static void ConvertBgraToRgba(byte[] src, byte[] dst, int width, int height)
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
                FontDescription = font
            };

            var width = Math.Max(GetLabelWidth(layout, r), minwidth);

			var surface = new Cairo.ImageSurface(
                Cairo.Format.ARGB32,
			    width + 2,
			    height + 2
            );

            var context = new Cairo.Context(surface)
            {
                LineWidth = 1
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

			var pixdata = new byte[] { Marshal.ReadByte(ret.Pixels) };

			ConvertBgraToRgba(data, pixdata, width + 2, height + 2);

			return ret;
		}
	}
}

// ex:ts=4 noet
