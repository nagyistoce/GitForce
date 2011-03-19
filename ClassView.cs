﻿using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GitForce.Properties;

namespace GitForce
{
    /// <summary>
    /// This class contains support functions and data definitions
    /// for the various views within forms.
    /// </summary>
    public static class ClassView
    {
        /// <summary>
        /// Enumeration of icons for files at different stage
        /// </summary>
        public enum Img {
            FileUnmodified,
            FileModified,
            FileAdded,
            FileDeleted,
            FileRenamed,
            FileCopied,
            FileUnmerged,
            FileUntracked,

            FolderClosed,
            FolderOpened,
            DatabaseClosed,
            DatabaseOpened,
            ChangelistRoot,
            Changelist,
        }

        /// <summary>
        /// Describes a mapping from image number into the resource icon
        /// </summary>
        private static readonly Dictionary<Img, System.Drawing.Icon> Res = new Dictionary<Img, System.Drawing.Icon> {
            { Img.FileUnmodified,   Resources.TreeIconU },
            { Img.FileModified,     Resources.TreeIconM },
            { Img.FileAdded,        Resources.TreeIconA },
            { Img.FileDeleted,      Resources.TreeIconD },
            { Img.FileRenamed,      Resources.TreeIconR },
            { Img.FileCopied,       Resources.TreeIconC },
            { Img.FileUnmerged,     Resources.TreeIconX },
            { Img.FileUntracked,    Resources.TreeIconQ },

            { Img.FolderClosed,     Resources.TreeIconFC },
            { Img.FolderOpened,     Resources.TreeIconFO },
            { Img.DatabaseClosed,   Resources.TreeIconDB },
            { Img.DatabaseOpened,   Resources.TreeIconDB },
            { Img.ChangelistRoot,   Resources.Change0 },
            { Img.Changelist,       Resources.Change1 },
        };

        /// <summary>
        /// Describes a mapping from a git file status code to the image associated with it
        /// </summary>
        private static readonly Dictionary<char, Img> Staticons = new Dictionary<char, Img> {
            { ' ', Img.FileUnmodified },
            { 'M', Img.FileModified },
            { 'A', Img.FileAdded },
            { 'D', Img.FileDeleted },
            { 'R', Img.FileRenamed },
            { 'C', Img.FileCopied },
            { 'U', Img.FileUnmerged },
            { '?', Img.FileUntracked },
        };

        /// <summary>
        /// Returns the image list to be used with a tree view showing the files
        /// </summary>
        public static ImageList GetImageList()
        {
            ImageList il = new ImageList();
            il.ImageSize = new System.Drawing.Size(32, 16);
            il.ColorDepth = ColorDepth.Depth32Bit;
            foreach (var kvp in Res)
                il.Images.Add(kvp.Value);

            return il;
        }

        /// <summary>
        /// Traverse tree nodes and assign an icon corresponding to each file status.
        /// Since each file has 2 status codes, X and Y, for index with relation to the
        /// git cache, and for current file with relation to the index, isIndex
        /// specifies which code will be used.
        /// </summary>
        public static void ViewAssignIcon(ClassStatus status, TreeNode rootNode, bool isIndex)
        {
            TreeNodeCollection nodes = rootNode.Nodes;
            foreach (TreeNode tn in nodes)
            {
                string name = tn.Tag.ToString();
                if (status.IsMarked(name))
                {
                    char icon = isIndex ? status.GetXcode(name) : status.GetYcode(name);
                    tn.ImageIndex = (int)Staticons[icon];
                }
                else
                {
                    tn.ImageIndex = isIndex ? (int)Img.Changelist : (int)Img.FolderClosed;
                }
                ViewAssignIcon(status, tn, isIndex);
            }
        }

        /// <summary>
        /// Recursive function to traverse the virtual directory of
        /// a flat git response files and build a visual tree component.
        /// 
        /// Root tree node needs to have its Tag set to the full directory
        /// path of the root location upon which the git list is based. This
        /// string will be a default prefix to all nodes' Tags.
        /// </summary>
        /// <param name="tnRoot">Root node to base the tree on</param>
        /// <param name="list">List of files in git format</param>
        /// <param name="sortBy">File sorting order</param>
        public static void BuildTreeRecurse(TreeNode tnRoot, List<string> list, GitDirectoryInfo.SortBy sortBy)
        {
            string fullPath = tnRoot.Tag.ToString();
            GitDirectoryInfo dir = new GitDirectoryInfo(fullPath, list);
            GitDirectoryInfo[] dirs = dir.GetDirectories();

            foreach (GitDirectoryInfo d in dirs)
            {
                TreeNode tn = new TreeNode(d.Name);
                tn.Tag = d.FullName + Path.DirectorySeparatorChar;
                tnRoot.Nodes.Add(tn);
                BuildTreeRecurse(tn, d.List, sortBy);
            }

            GitFileInfo[] files = dir.GetFiles(sortBy);

            foreach (GitFileInfo file in files)
            {
                TreeNode tn = new TreeNode(file.Name);
                tn.Tag = file.FullName;
                tnRoot.Nodes.Add(tn);
            }
        }

        /// <summary>
        /// Builds a flat list of tree nodes based on the given list of files
        /// </summary>
        /// <param name="tnRoot">Root node to build the list on</param>
        /// <param name="root">Root path to the repo these files refer to</param>
        /// <param name="list">List of relative path file names</param>
        public static void BuildFileList(TreeNode tnRoot, string root, List<string> list)
        {
            // Build a list view
            foreach (string s in list)
            {
                TreeNode tn = new TreeNode(s);
                tn.Tag = Path.Combine(root, s);
                tnRoot.Nodes.Add(tn);
            }
        }

        /// <summary>
        /// Build a tree view of commit nodes using hints from repo commit groups
        /// </summary>
        public static TreeNode BuildCommitsView(ClassRepo repo, List<string> list)
        {
            repo.Commits.Rebuild(list);

            // Step 1: Build the default list with all files under this status class
            TreeNode root = new TreeNode("Pending Changelists");
            root.Tag = "root";                      // Tag of the root node

            foreach (var c in repo.Commits.Bundle)
            {
                TreeNode commitNode = new TreeNode(c.Description);
                commitNode.Tag = c;

                foreach (var f in c.Files)
                {
                    TreeNode fNode = new TreeNode(f);
                    fNode.Tag = Path.Combine(repo.Root, f);
                    commitNode.Nodes.Add(fNode);
                }
                root.Nodes.Add(commitNode);
            }
            root.ExpandAll();
            return root;
        }
    }
}