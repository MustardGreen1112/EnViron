using System;
using System.Collections.Generic;
using Newtonsoft.Json;
namespace VascularGenerator.DataStructures
{
    public class Tree<T> where T : IsCopyable<T>
    {   
        [JsonProperty] public int ID { get; private set; }
        
        [JsonProperty] public T value { get; set; }

        [JsonProperty] public List<Tree<T>> children { get; set; } = new List<Tree<T>>();

        [JsonIgnore] public Tree<T> parent { get; set; }

        static int IDMaster = 0;
        [JsonProperty] public bool isKey = false;
        [JsonProperty] public string tag = "";

        public bool visited = false;

        public Tree(T value)
        {
            this.value = value;
            children = new List<Tree<T>>();
            parent = null;
            ID = IDMaster;
            IDMaster += 1;
        }

        public void AddChild(Tree<T> child)
        {
            children.Add(child);
            child.parent = this;
        }

        public void SetChildren(List<Tree<T>> children)
        {
            this.children = children;
            foreach (Tree<T> child in children)
            {
                child.parent = this;
            }
        }

        public void SetParent(Tree<T> parent)
        {

            this.parent = parent;
            parent.AddChild(this);

        }

        public void SetValue(T value)
        {
            this.value = value;
        }

        public List<Tree<T>> GetChildren()
        {
            return children;
        }

        public Tree<T> GetParent()
        {
            return parent;
        }

        public T GetValue()
        {
            return value;
        }

        public int GetID()
        {
            return ID;
        }

        public bool IsLeaf()
        {
            return children.Count == 0;
        }

        public Tree<T> GetRoot()
        {
            if (this.parent == null)
            {
                return this;
            }
            else
            {
                return this.parent.GetRoot();
            }
        }
        
        public Tree<T> CreateCopy()
        {
            Tree<T> copy = new Tree<T>(value.CreateCopy());
            copy.ID = this.ID;
            if (this.children.Count == 0) //if there are no children, we return a copy of this node
            {
                return copy;
            }
            else //if there are children we create a copy of each child and add them as children to the copy of this node, then return the copy of this node
            {
                foreach (Tree<T> c in this.children)
                {
                    copy.AddChild(c.CreateCopy());
                }
                return copy;
            }
        }

        //returns the number of nodes in the tree
        public int GetNumberOfNodes()
        {
            if (this.children.Count == 0)
            {
                return 1;
            }
            
            int numberOfNodes = 1;
            foreach (Tree<T> c in children)
            {
                numberOfNodes += c.GetNumberOfNodes();
            }

            return numberOfNodes;
        }

        //function that returns a random node in the tree
        public Tree<T> GetRandomNode()
        {   
            Random random = new Random();
            int count = random.Next(this.GetNumberOfNodes());
            Tree<T> output = this;
            List<Tree<T>> toVisit = new(){this};
            while (toVisit.Count > 0)
            {
                Tree<T> current = toVisit[0];
                toVisit.RemoveAt(0);

                if (count == 0)
                {
                    output = current;
                    break;
                }
                else
                {
                    count--;
                }
                if (current.GetChildren().Count ==0){continue;}
                foreach(Tree<T> c in current.GetChildren()){toVisit.Add(c);}
            }

            return output;

        }

        //returns a list of nodes in the tree that satisfy the node significance requirement. (it calculates the number of nodes down the line from this node and that is the node's "significance", only nodes that satisfy the nodeSignificance requirement (ie. they have more than nodeSignificance children) are returned in the list).
        public List<Tree<T>> GetKeyNodes(int nodeSignificance)
        {
            List<Tree<T>> output = new();
            List<Tree<T>> toVisit = new(){this};
            while (toVisit.Count > 0)
            {
                Tree<T> current = toVisit[0];
                toVisit.RemoveAt(0);
                if (current.GetNumberOfNodes() == nodeSignificance){output.Add(current); continue;}
                if (current.GetChildren().Count == 0) {continue;}
                foreach (Tree<T> c in current.GetChildren()){toVisit.Add(c);}
            }

            return output;
        }

        public override string ToString()
        {
            string output = "Tree Node:\n"+ value.ToString();
            if (children.Count == 0)
            {
                return value.ToString() + "\n--noChildren--";
            }
            else
            {
                foreach (Tree<T> c in children)
                {
                    output += "\n\tChild:\n" + c.ToString();
                }
            }
            return output;
        }

    }
}
