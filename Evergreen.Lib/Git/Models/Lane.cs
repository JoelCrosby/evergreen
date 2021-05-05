using System;
using System.Collections.Generic;

using LibGit2Sharp;

namespace Evergreen.Lib.Git.Models
{
    [Flags]
    public enum LaneTag
    {
        NONE = 0,
        START = 1 << 0,
        END = 1 << 1,
        SIGN_STASH = 1 << 2,
        SIGN_STAGED = 1 << 3,
        SIGN_UNSTAGED = 1 << 4,
        HIDDEN = 1 << 5
    }

    public class Lane
    {
        public Color Color { get; set; }
        public List<int> From { get; set; }
        public LaneTag Tag { get; set; }
        public ObjectId BoundaryId { get; set; }

        public Lane() => WithColor(null);

        public Lane WithColor(Color color)
        {
            Color = color ?? Color.Next();

            return this;
        }

        public Lane Copy()
        {
            var ret = new Lane().WithColor(Color);
            ret.From = new List<int>(From);
            ret.Tag = Tag;
            ret.BoundaryId = BoundaryId;

            return ret;
        }

        public Lane Dup()
        {
            var ret = new Lane().WithColor(Color.Copy());
            ret.From = new List<int>(From);
            ret.Tag = Tag;
            ret.BoundaryId = BoundaryId;

            return ret;
        }
    }
}
