using System.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

using LibGit2Sharp;

namespace Evergreen.Lib.Git.Models
{
    public class CommitModel
    {
        public LaneTag Tag { get; set; }

        private uint laneIndex;
        private List<Lane> lanes = new ();

        public Commit Data;

        public CommitModel(Commit c)
        {
            Data = c;
        }

        public List<Lane> GetLanes()
        {
            return lanes;
        }

        public uint MyLane
        {
            get => laneIndex;
            set
            {
                laneIndex = value;
                UpdateLaneTag();
            }
        }

        public Lane Lane => lanes.ElementAtOrDefault((int)laneIndex);

        public List<Lane> InsertLane(Lane lane, int idx)
        {
            lanes.Insert(idx, lane);
            return lanes;
        }

        public List<Lane> RemoveLane(Lane lane)
        {
            lanes.Remove(lane);
            return lanes;
        }

        private void UpdateLaneTag()
        {
            var lane = lanes.ElementAtOrDefault((int)laneIndex);

            if (lane == null)
            {
                return;
            }

            lane.Tag &= ~(LaneTag.SIGN_STASH |
                        LaneTag.SIGN_STAGED |
                        LaneTag.SIGN_UNSTAGED) | Tag;
        }

        public void UpdateLanes(List<Lane> lanes, int mylane)
        {
            this.lanes = lanes;

            if (mylane >= 0)
            {
                laneIndex = (ushort)mylane;
            }

            UpdateLaneTag();
        }
    }
}
