using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraft.InputSystem;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ScriptableObjects.AlchemyMachineProducts;

namespace xiaoye97
{
    public class SortBookmark : MonoBehaviour
    {
        private Button hotkeyButton;

        private static List<string> oriRailNames = new List<string>()
        {
            "LeftToRight1",
            "LeftToRight2",
            "TopToBottom1",
            "RightToLeft1",
            "RightToLeft2",
            "BottomToTop1"
        };

        private void Awake()
        {
            hotkeyButton = KeyboardKey.Get(SortBookmarkPlugin.Hotkey.Value);
        }

        private void Update()
        {
            if (hotkeyButton.State == State.JustDowned)
            {
                Debug.Log("按下了整理快捷键");
                if (RecipeBook.Instance != null && RecipeBook.Instance.gameObject.activeInHierarchy)
                {
                    SortAllBookmark();
                }
            }
        }

        public void SortAllBookmark()
        {
            var groupCtl = RecipeBook.Instance.bookmarkControllersGroupController;
            var markCtl = groupCtl.controllers[0].bookmarkController;

            // Get a list of all bookmarks before sort for use later
            var preSortBookmarkList = groupCtl.GetAllBookmarksList();

            // Get all bookmarks on the non-modded rails
            var allRails = groupCtl.controllers[0].bookmarkController.rails;
            var nonModdedRails = allRails.Take(6);
            var marks = nonModdedRails.SelectMany(r => r.railBookmarks).ToList();
            // 所有书签
            List<PotionAndBookmark> all = new List<PotionAndBookmark>();
            for (int i = 0; i < marks.Count; i++)
            {
                PotionAndBookmark pb = new PotionAndBookmark();
                pb.Bookmark = marks[i];
                pb.IsEmpty = RecipeBook.Instance.IsEmptyPage(i);
                if (!pb.IsEmpty)
                {
                    pb.Content = RecipeBook.Instance.GetPageContent(i);
                    if (pb.Content is Potion potion)
                    {
                        pb.Potion = potion;
                        pb.CustomDescription = potion.CustomDescription;
                        pb.IsPotionMark = true;
                    }
                    else if (pb.Content is AlchemyMachineProduct amp)
                    {
                        pb.CustomDescription = amp.CustomDescription;
                    }
                }
                all.Add(pb);
            }
            // 挑出需要排序的书签
            List<PotionAndBookmark> sortPbs = new List<PotionAndBookmark>();
            foreach (var pb in all)
            {
                // 如果不为空并且不跳过并且rail是原版的rail，则加到列表
                if (!pb.IsEmpty && oriRailNames.Contains(pb.Bookmark.rail.name))
                {
                    if (!string.IsNullOrWhiteSpace(pb.CustomDescription) && !pb.CustomDescription.StartsWith("skip"))
                    {
                        sortPbs.Add(pb);
                    }
                }
            }
            // 挑出空书签
            List<PotionAndBookmark> emptyPbs = new List<PotionAndBookmark>();
            foreach (var pb in all)
            {
                // 如果为空，则加到列表
                if (pb.IsEmpty)
                {
                    emptyPbs.Add(pb);
                }
            }
            // 排序
            SortRailBookmark(sortPbs);

            Debug.Log($"共有{all.Count}个书签，其中{sortPbs.Count}个有内容书签需要整理，{emptyPbs.Count}个空书签");
            int index = 0;
            // 一轨一轨的循环放置书签
            foreach (var rail in markCtl.rails)
            {
                // 如果不是原版的rail，则跳过
                if (!oriRailNames.Contains(rail.name))
                {
                    continue;
                }
                float usedX = 0;
                while (sortPbs.Count > 0)
                {
                    var pb = sortPbs[0];
                    var mark = pb.Bookmark;
                    var markSize = mark.GetBookMarkSize();
                    // 如果已使用的空间+新的书签的空间大于轨道空间，则切换到下一个轨道
                    if (usedX + markSize.x > rail.size.x)
                    {
                        break;
                    }
                    // 交错放置
                    if (index % 2 == 0)
                    {
                        Vector2 pos = new Vector2((usedX + markSize.x / 2) / (rail.size.x), 0);
                        rail.Connect(mark, pos);
                    }
                    else
                    {
                        Vector2 pos = new Vector2((usedX + markSize.x / 2 + 0.05f) / (rail.size.x), 1);
                        rail.Connect(mark, pos);
                        usedX += markSize.x;
                    }
                    sortPbs.RemoveAt(0);
                    index++;
                }
            }

            var lastRail = markCtl.rails.Find(r => r.name == "BottomToTop1");
            // 如果有多余的书签，一起堆放到最后一个轨道
            if (sortPbs.Count > 0)
            {
                int stackIndex = 0;
                foreach (var pb in sortPbs)
                {
                    lastRail.Connect(pb.Bookmark, new Vector2(stackIndex * 0.01f, (stackIndex % 10) / 10f));
                    stackIndex++;
                }
                Debug.Log($"将{sortPbs.Count}个书签堆放在了最后一个轨道");
            }
            // 将空书签堆放到最后一个轨道的后方
            if (emptyPbs.Count > 0)
            {
                foreach (var pb in emptyPbs)
                {
                    lastRail.Connect(pb.Bookmark, new Vector2(1, 1));
                }
                Debug.Log($"将{emptyPbs.Count}个空书签堆放在了最后一个轨道");
            }

            // Trigger the bookmarks rearranged method to reorganize the savedRecipes list to match the new bookmark order
            markCtl.CallOnBookmarksRearrangeIfNecessary(preSortBookmarkList);
        }

        /// <summary>
        /// 排序轨道上的书签
        /// </summary>
        /// <param name="all"></param>
        public void SortRailBookmark(List<PotionAndBookmark> all)
        {
            all.Sort((a, b) =>
            {
                // 有一个为空
                if (a.IsEmpty && !b.IsEmpty)
                {
                    return 1;
                }
                if (!a.IsEmpty && b.IsEmpty)
                {
                    return -1;
                }
                // Hash比较
                int hashA = a.GetHashCode();
                int hashB = b.GetHashCode();
                // 空书签比较
                if (a.IsEmpty && b.IsEmpty)
                {
                    return hashA.CompareTo(hashB);
                }
                // 非药水比较
                if (a.IsPotionMark && !b.IsPotionMark)
                {
                    return 1;
                }
                if (!a.IsPotionMark && b.IsPotionMark)
                {
                    return -1;
                }
                if (a.IsPotionMark && b.IsPotionMark)
                {
                    // 药水最高等级比较
                    int aMaxTier = a.Potion.GetMaxTier();
                    int bMaxTier = b.Potion.GetMaxTier();
                    if (aMaxTier != bMaxTier)
                    {
                        return aMaxTier.CompareTo(bMaxTier);
                    }

                    // 药水总等级比较
                    int aAllTier = a.Potion.GetCollapsedEffects().Values.Sum();
                    int bAllTier = b.Potion.GetCollapsedEffects().Values.Sum();
                    if (aAllTier != bAllTier)
                    {
                        return aAllTier.CompareTo(bAllTier);
                    }

                    // 药水价格比较
                    float aPrice = a.Potion.GetPrice();
                    float bPrice = b.Potion.GetPrice();
                    if (aPrice != bPrice)
                    {
                        return aPrice.CompareTo(bPrice);
                    }

                    // 药水排序ID比较
                    if (a.Potion.sortingId != b.Potion.sortingId)
                    {
                        return a.Potion.sortingId.CompareTo(b.Potion.sortingId);
                    }
                }

                return hashA.CompareTo(hashB);
            });
        }
    }
}