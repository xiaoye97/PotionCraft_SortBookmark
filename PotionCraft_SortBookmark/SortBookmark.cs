using BepInEx;
using System.Linq;
using UnityEngine;
using BepInEx.Configuration;
using PotionCraft.ManagersSystem;
using System.Collections.Generic;
using PotionCraft.ScriptableObjects.Potion;

namespace xiaoye97
{
    [BepInPlugin("me.xiaoye97.plugin.PotionCraft.SortBookmark", "SortBookmark", "1.0.0")]
    public class SortBookmark : BaseUnityPlugin
    {
        private ConfigEntry<KeyCode> hotkey;

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
            var marks = groupCtl.GetAllBookmarksList();
            List<PotionAndBookmark> all = new List<PotionAndBookmark>();
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
            // 排序
            SortFunc1(all);
            // 将合成书的书页按新的排放
            recipeBook.savedRecipes.Clear();
            foreach (var pb in all)
            {
                if (pb.IsEmpty)
                {
                    recipeBook.savedRecipes.Add(null);
                }
                else
                {
                    recipeBook.savedRecipes.Add(pb.Potion);
                }
            }

            Debug.Log($"共有{marks.Count}个书签");
            int index = 0;
            // 一轨一轨的循环
            foreach (var rail in markCtl.rails)
            {
                float usedX = 0;
                while (all.Count > 0)
                {
                    var pb = all[0];
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
                    all.RemoveAt(0);
                    index++;
                }
            }
            // 如果有多余的书签，则堆放到最后一个轨道
            if (all.Count > 0)
            {
                var lastRail = markCtl.rails[markCtl.rails.Count - 1];
                foreach (var pb in all)
                {
                    lastRail.Connect(pb.Bookmark, new Vector2(1, 1));
                }
                Debug.Log($"将{all.Count}个书签堆放在了最后一个轨道");
            }
        }

        /// <summary>
        /// 第一种排序方式
        /// </summary>
        /// <param name="all"></param>
        public void SortFunc1(List<PotionAndBookmark> all)
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