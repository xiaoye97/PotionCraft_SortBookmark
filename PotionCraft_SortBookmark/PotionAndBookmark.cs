using System;
using System.Collections.Generic;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;

namespace xiaoye97
{
    public class PotionAndBookmark
    {
        public IBookPageContent Content;
        public Potion Potion;
        public Bookmark Bookmark;
        public bool IsEmpty;
        public bool IsPotionMark;
        public string CustomDescription;
    }
}
