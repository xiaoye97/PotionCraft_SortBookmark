using BepInEx;
using System.Linq;
using UnityEngine;
using BepInEx.Configuration;
using PotionCraft.ManagersSystem;
using System.Collections.Generic;
using PotionCraft.ScriptableObjects.Potion;

namespace xiaoye97
{
    [BepInPlugin("me.xiaoye97.plugin.PotionCraft.SortBookmark", "SortBookmark", "1.2.0")]
    public class SortBookmark : BaseUnityPlugin
    {
        private ConfigEntry<KeyCode> hotkey;
        private static List<string> oriRailNames = new List<string>()
        {
            "LeftToRight1",
            "LeftToRight2",
            "TopToBottom1",
            "RightToLeft1",
            "RightToLeft2",
            "BottomToTop1"
        };

        private void Start()
        {
            hotkey = Config.Bind<KeyCode>("config", "SortHotkey", KeyCode.Space);
        }

        private void Update()
        {
            if (Input.GetKeyDown(hotkey.Value))
            {
                if (Managers.Potion.recipeBook.gameObject.activeInHierarchy)
                {
                    SortAllBookmark();
                }
            }
        }

        public void SortAllBookmark()
        {
            var recipeBook = Managers.Potion.recipeBook;
            var groupCtl = recipeBook.bookmarkControllersGroupController;
            var markCtl = groupCtl.controllers[0].bookmarkController;

            // 所有书签
            List<PotionAndBookmark> all = new List<PotionAndBookmark>();

            // Get a list of all bookmarks before sort for use later
            var preSortBookmarkList = groupCtl.GetAllBookmarksList();

            // Get all bookmarks on the non-modded rails
            var allRails = groupCtl.controllers[0].bookmarkController.rails;
            var nonModdedRails = allRails.Take(6);
            var marks = nonModdedRails.SelectMany(r => r.railBookmarks).ToList();
            for (int i = 0; i < marks.Count; i++)
            {
                PotionAndBookmark pb = new PotionAndBookmark();
                pb.Bookmark = marks[i];
                pb.IsEmpty = recipeBook.IsEmptyPage(i);
                if (!pb.IsEmpty)
                {
                    pb.Potion = (Potion)recipeBook.GetPageContent(i);
                }
                all.Add(pb);
            }
            // 挑出需要排序的书签
            List<PotionAndBookmark> sortPbs = new List<PotionAndBookmark>();
            foreach (var pb in all)
            {
                // 如果不为空并且不跳过并且rail是原版的rail，则加到列表
                if (!pb.IsEmpty && !pb.Potion.customDescription.StartsWith("skip") && oriRailNames.Contains(pb.Bookmark.rail.name))
                {
                    sortPbs.Add(pb);
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


            // 书签排序完毕，重新排序书页
            // 从此时的轨道中，依次取出书签对应的书页，由于rail在连接时会自动重排，所以直接添加即可

            // This code is no longer nessesary
            //recipeBook.savedRecipes.Clear();
            //foreach (var rail in markCtl.rails)
            //{
            //    foreach (var bookmark in rail.railBookmarks)
            //    {
            //        // 查找对应的页面
            //        for (int i = all.Count - 1; i >= 0; i--)
            //        {
            //            // 找到了
            //            if (all[i].Bookmark == bookmark)
            //            {
            //                recipeBook.savedRecipes.Add(all[i].Potion);
            //                all.RemoveAt(i);
            //                break;
            //            }
            //        }
            //    }
            //}
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
                if (b.IsEmpty && !a.IsEmpty)
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

                return hashA.CompareTo(hashB);
            });
        }
    }
}