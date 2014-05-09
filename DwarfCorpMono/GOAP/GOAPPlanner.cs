using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{

    public class GOAPPlanner
    {
        public class GOAPNode : IEquatable<GOAPNode>
        {
            public WorldState m_state;
            public Action m_actionTaken;

            public bool Equals(GOAPNode other)
            {
                return m_state.Equals(other.m_state);
            }

            public override bool Equals(object obj)
            {
                if (obj is GOAPNode)
                {
                    return Equals((GOAPNode)obj);
                }
                else
                {
                    return false;
                }
            }

            public override int GetHashCode()
            {
                return m_state.GetHashCode();
            }
        }

        public GOAP Agent { get; set; }

        public GOAPPlanner(GOAP agent)
        {
            Agent = agent;
        }

        public GOAPNode GetNodeWithMinimumFScore(PriorityQueue<GOAPNode> f_score, HashSet<GOAPNode> openSet)
        {
            return f_score.Dequeue();
        }


        public  List<GOAPNode> ReconstructPath(Dictionary<GOAPNode, GOAPNode> cameFrom, GOAPNode currentNode)
        {
            List<GOAPNode> toReturn = new List<GOAPNode>();
            if (cameFrom.ContainsKey(currentNode))
            {
                toReturn.AddRange(ReconstructPath(cameFrom, cameFrom[currentNode]));
                toReturn.Add(currentNode);
                return toReturn;
            }
            else
            {
                toReturn.Add(currentNode);
                return toReturn;
            }

        }

        public  List<GOAPNode> ComputeNeighbors(GOAPNode node)
        {
            List<Action> action = Agent.GetPossibleActions(node.m_state);
            List<GOAPNode> toReturn = new List<GOAPNode>();
            foreach (Action a in action)
            {
                WorldState state = new WorldState(node.m_state);
                a.Apply(state);
                GOAPNode n = new GOAPNode();
                n.m_state = state;
                n.m_actionTaken = a;
                toReturn.Add(n);

            }

            return toReturn;
        }

        public List<GOAPNode> ComputeReverseNeighbors(GOAPNode node)
        {
            List<Action> action = Agent.GetActionsSatisfying(node.m_state);
            List<GOAPNode> toReturn = new List<GOAPNode>();
            foreach (Action a in action)
            {
                WorldState state = new WorldState(node.m_state);
                a.UnApply(state);
                GOAPNode n = new GOAPNode();
                n.m_state = state;
                n.m_actionTaken = a;
                toReturn.Add(n);

            }

            return toReturn;
        }

        private  bool Path(GOAPNode start, GOAPNode end, ChunkManager chunks, int maxExpansions, ref List<GOAPNode> toReturn)
        {
            HashSet<GOAPNode> closedSet = new HashSet<GOAPNode>();

            HashSet<GOAPNode> openSet = new HashSet<GOAPNode>();
            openSet.Add(end);

            Dictionary<GOAPNode, GOAPNode> cameFrom = new Dictionary<GOAPNode, GOAPNode>();
            Dictionary<GOAPNode, float> g_score = new Dictionary<GOAPNode, float>();
            PriorityQueue<GOAPNode> f_score = new PriorityQueue<GOAPNode>();
            g_score[end] = 0.0f;
            f_score.Enqueue(end, g_score[end] + Heuristic(start, end));

            GOAPNode current = end;

            int numExpansions = 0;
            while (openSet.Count > 0 && numExpansions < maxExpansions)
            {

                current = GetNodeWithMinimumFScore(f_score, openSet);

                
                
                //if(current.m_actionTaken != null)
                //    Console.Out.WriteLine("Considering Action: {0}", current.m_actionTaken.Name);
                         
               
                
                //SimpleDrawing.DrawBox(current.GetBoundingBox(), Color.Red, 0.1f);

                numExpansions++;
                if (start.m_state.MeetsRequirements(current.m_state))
                {
                    toReturn = ReconstructPath(cameFrom, current);
                    return true;
                }

                openSet.Remove(current);
                closedSet.Add(current);


                List<GOAPNode> neighbors = ComputeReverseNeighbors(current);

                foreach (GOAPNode n in neighbors)
                {

                    if(start.m_state.MeetsRequirements(n.m_state))
                    {
                        List<GOAPNode> subPath = ReconstructPath(cameFrom, current);
                        start.m_actionTaken = n.m_actionTaken;
                        subPath.Add(start);
                        toReturn = subPath;
                        return true;
                    }

                    if (closedSet.Contains(n) || n.Equals(current))
                    {
                        continue;
                    }

                    float tenative_g_score = g_score[current] + GetDistance(current, n);

                    if (!openSet.Contains(n) || tenative_g_score < g_score[n])
                    {
                        openSet.Add(n);
                        cameFrom[n] = current;
                        g_score[n] = tenative_g_score;
                        f_score.Enqueue(n, g_score[n] + Heuristic(start, n));
                    }
                }

                if (numExpansions >= maxExpansions)
                {
                    List<GOAPNode> subPath = ReconstructPath(cameFrom, current);
                    toReturn = subPath;
                    return false;
                }

            }
            toReturn = null;
            return false;

        }


        public List<GOAPNode> FindPath(GOAPNode start, GOAPNode end, ChunkManager chunks, int maxExpansions)
        {
            List<GOAPNode> p = new List<GOAPNode>();
            bool success = Path(start, end, chunks, maxExpansions, ref p);
            if (p != null && success)
            {
                p.Reverse();
                return p;
            }
            else
            {
                return null;
            }

        }

        public  float GetDistance(GOAPNode A, GOAPNode B)
        {
            if (B.m_actionTaken == null)
            {
                return 1.0f;
            }
            else
            {
                return B.m_actionTaken.Cost;
            }
        }

        public float Heuristic(GOAPNode A, GOAPNode B)
        {
            return A.m_state.Distance(B.m_state);
        }
    }


}
