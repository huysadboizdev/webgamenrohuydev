using System;
using System.Collections.Generic;
using System.Linq;

namespace Game1
{
    internal class AutoItem : IChatable
    {
        internal static List<AutoItem> listItemUses = new List<AutoItem>();
        internal int ID { get; set; }
        internal int timeDelay { get; set; }
        internal int indexUI { get; set; }
        internal long lastTimeUseItem { get; set; }
        private string optionKey = "";
        private bool hasUsed;
        internal long remainingUses = -1;
        private int setupStage;
        private long requestedUses;
        private bool showNextPrompt;
        private static AutoItem pending;
        private static long pendingQuantity;
        private static long pendingSince;
        private static long lastBeanUse;
        private static int bagRevision, sentRevision, nextTurn;
        private static AutoItem instance;
        private static readonly HashSet<int> giftIds = new HashSet<int> {570,648,722,736,962,963,1170,1171,1560,1569,1591,1592,1594,1608,1655,1757,1775,1776,1777,1821,1840};
        private static readonly HashSet<int> consumableIds = new HashSet<int> {211,212,342,343,344,345,361,379,380,381,382,383,384,385,460,579,638,663,664,665,666,667,726,727,728,764,880,881,882,962,963,1045,1150,1151,1152,1153,1154,1233,1532,1575,1576,1609,1614,1615,1616,1628,1635,1822,1823};
        internal static AutoItem gI() { return instance ?? (instance = new AutoItem()); }
        internal AutoItem() { }
        internal AutoItem(int id, int index, int seconds, bool isGold = false, bool isGem = false)
        {
            ID = id; indexUI = index; timeDelay = seconds;
            Item item = Char.myCharz().arrItemBag.FirstOrDefault(i => i != null && i.template.id == id && i.indexUI == index);
            if (item != null) optionKey = Options(item);
        }
        private static string Options(Item item)
        {
            if (item.itemOption == null) return "";
            return string.Join(";", item.itemOption.Where(o => o != null && o.optionTemplate != null)
                .Select(o => o.optionTemplate.id + ":" + o.param).OrderBy(s => s, StringComparer.Ordinal).ToArray());
        }
        private bool Matches(Item item) { return item != null && item.quantity > 0 && item.template.id == ID && Options(item) == optionKey; }
        private long QuantityInBag() { return Char.myCharz().arrItemBag.Where(Matches).Sum(i => (long)i.quantity); }
        internal static bool IsEnabled(int id) { return listItemUses.Any(i => i.ID == id); }
        internal static bool CanAuto(Item item)
        {
            if (item == null || item.quantity <= 0 || item.isTypeBody() || item.template.type == 24) return false;
            if (item.template.type == 29 || item.template.type == 33 || item.template.type == 6) return true;
            return giftIds.Contains(item.template.id) || consumableIds.Contains(item.template.id);
        }
        internal static void Start(Item item)
        {
            if (!CanAuto(item)) return;
            AutoItem prompt = gI();
            prompt.ID = item.template.id; prompt.indexUI = item.indexUI; prompt.optionKey = Options(item);
            prompt.setupStage = 1; prompt.requestedUses = 0; prompt.showNextPrompt = false;
            GameCanvas.panel.hideNow();
            ChatTextField tf = ChatTextField.gI();
            tf.strChat = "Số lần/cái dùng " + item.template.name;
            tf.tfChat.name = "Số lần/cái sử dụng (1 - " + prompt.QuantityInBag() + ")";
            tf.tfChat.setIputType(TField.INPUT_TYPE_NUMERIC);
            tf.startChat2(prompt, string.Empty);
        }
        internal static void Stop(int id)
        {
            listItemUses.RemoveAll(i => i.ID == id);
            ModFunc.GI().listItemAuto.RemoveAll(i => i.id == id);
        }
        internal static void Clear()
        {
            listItemUses.Clear(); ModFunc.GI().listItemAuto.Clear(); pending = null; nextTurn = 0; lastBeanUse = 0;
            if (instance != null) { instance.setupStage = 0; instance.requestedUses = 0; instance.showNextPrompt = false; }
        }
        internal static void OnBagUpdated() { bagRevision++; }
        internal static void NoteBeanUse() { lastBeanUse = mSystem.currentTimeMillis(); }
        private static int RequiredSlots(int id)
        {
            switch (id)
            {
                case 1609: case 1821: case 1840: return 5;
                case 736: case 1591: case 1592: case 1594: case 1757: case 1776: case 1777: return 2;
                case 380: case 1822: case 1823: return 1;
                default: return giftIds.Contains(id) ? 1 : 0;
            }
        }
        private static bool WouldExceedBuffLimit(Item item)
        {
            int icon, dose = 600;
            switch (item.template.id)
            {
                case 381: icon = 2754; break;
                case 382: icon = 2755; break;
                case 383: icon = 2756; break;
                case 384: icon = 2757; break;
                case 385: icon = 2760; break;
                case 1150: icon = 10716; break;
                case 1151: icon = 10715; break;
                case 1152: icon = 10714; break;
                case 1153: icon = 10712; break;
                case 1154: icon = 10717; break;
                case 638: icon = 5829; dose = 3600; break;
                case 1233: icon = 11173; dose = 1800; break;
                case 579: case 1045: icon = 5072; dose = 1800; break;
                case 1635: icon = 13618; dose = 1800; break;
                case 1614: icon = 13462; break;
                case 1615: icon = 13463; break;
                case 1616: icon = 13464; break;
                case 1628: icon = 13540; dose = 1800; break;
                case 663: case 664: case 665: case 666: case 667:
                case 880: case 881: case 882: icon = item.template.iconID; break;
                default: return false;
            }
            ItemTime timer = ItemTime.getItemById(icon);
            return timer != null && System.Math.Max(0, timer.coutTime) + dose > 30000;
        }
        internal static void OnServerNotice(string text)
        {
            if (pending == null || string.IsNullOrEmpty(text)) return;
            string notice = text.ToLowerInvariant();
            if (notice.Contains("không thể") || notice.Contains("không đủ") || notice.Contains("bạn không") || notice.Contains("hành trang")
                || notice.Contains("chỉ có thể") || notice.Contains("chỉ được") || notice.Contains("tối đa 500") || notice.Contains("hãy chờ")
                || notice.Contains("đã đạt giới hạn") || notice.Contains("kết thúc giao dịch")) PausePending();
        }
        internal static void PausePending()
        {
            if (pending == null) return;
            int id = pending.ID; pending = null; Stop(id);
            GameScr.info1.addInfo("Đã dừng auto để bạn xử lý thông báo hoặc chọn tiếp.", 0);
        }
        internal static void Update()
        {
            // Open step two on the next frame: mobile keyboard callbacks close step one after onChatFromMe.
            if (instance != null && instance.showNextPrompt)
            {
                if (!Session_ME.gI().isConnected()) { Clear(); return; }
                if (GameCanvas.currentScreen is GameScr && !Char.isLoadingMap && GameCanvas.currentDialog == null)
                {
                    instance.showNextPrompt = false;
                    ChatTextField tf = ChatTextField.gI();
                    tf.strChat = "Khoảng nghỉ (giây) - " + ItemTemplates.get((short)instance.ID).name;
                    tf.tfChat.name = "Khoảng nghỉ giữa mỗi lần (giây, 0 = nhanh nhất)";
                    tf.tfChat.setIputType(TField.INPUT_TYPE_NUMERIC);
                    tf.startChat2(instance, string.Empty);
                }
                return;
            }
            if (listItemUses.Count == 0 && pending == null) return;
            if (!Session_ME.gI().isConnected()) { Clear(); return; }
            if (!(GameCanvas.currentScreen is GameScr) || Char.isLoadingMap) return;
            long now = mSystem.currentTimeMillis();
            if (pending != null)
            {
                if (bagRevision != sentRevision && pending.QuantityInBag() < pendingQuantity)
                {
                    AutoItem confirmed = pending;
                    pending = null;
                    if (confirmed.remainingUses > 0 && listItemUses.Contains(confirmed))
                    {
                        confirmed.remainingUses--;
                        if (confirmed.remainingUses == 0)
                        {
                            Stop(confirmed.ID);
                            GameScr.info1.addInfo("Đã dùng đủ số lần yêu cầu, auto đã dừng.", 0);
                        }
                    }
                }
                else if (now - pendingSince >= 3000)
                {
                    int id = pending.ID; pending = null; Stop(id);
                    GameScr.info1.addInfo("Đã dừng auto: chưa nhận kết quả sử dụng vật phẩm.", 0);
                    return;
                }
                else return;
            }
            if (GameCanvas.currentDialog != null || GameCanvas.menu.showMenu || ChatTextField.gI().isShow || Char.myCharz().meDead) return;
            int count = listItemUses.Count;
            for (int n = 0; n < count && listItemUses.Count > 0; n++)
            {
                int position = nextTurn % listItemUses.Count;
                AutoItem auto = listItemUses[position];
                nextTurn = (position + 1) % listItemUses.Count;
                Item item = Char.myCharz().arrItemBag.FirstOrDefault(auto.Matches);
                if (item == null) { Stop(auto.ID); nextTurn = position; continue; }
                if (WouldExceedBuffLimit(item))
                {
                    Stop(auto.ID); nextTurn = position;
                    GameScr.info1.addInfo("Đã dừng auto: lần dùng tiếp sẽ vượt giới hạn 500 phút.", 0);
                    continue;
                }
                if (Char.myCharz().arrItemBag.Count(i => i == null) < RequiredSlots(auto.ID))
                {
                    Stop(auto.ID); nextTurn = position;
                    GameScr.info1.addInfo("Đã dừng auto: hành trang không đủ ô trống nhận thưởng.", 0);
                    continue;
                }
                if (item.template.type == 6 && now - lastBeanUse < 1000) continue;
                long delay = System.Math.Max((long)auto.timeDelay * 1000, item.template.type == 6 ? 1000L : 0L);
                if (auto.hasUsed && now - auto.lastTimeUseItem < delay) continue;
                auto.indexUI = item.indexUI; auto.hasUsed = true; auto.lastTimeUseItem = now;
                pending = auto; pendingQuantity = auto.QuantityInBag(); pendingSince = now; sentRevision = bagRevision;
                if (item.template.type == 6) NoteBeanUse();
                Service.gI().useItem(0, 1, auto.indexUI, -1);
                return;
            }
        }
        public void onChatFromMe(string text, string to)
        {
            if (setupStage == 1)
            {
                long count;
                long available = QuantityInBag();
                if (!long.TryParse(text, out count) || count < 1 || count > available)
                {
                    GameCanvas.startOKDlg("Nhập số lần/cái từ 1 đến " + available + ".");
                    return;
                }
                requestedUses = count;
                setupStage = 2;
                showNextPrompt = true;
                ResetTF(ChatTextField.gI());
                return;
            }
            if (setupStage != 2) return;
            int seconds;
            if (!int.TryParse(text, out seconds) || seconds < 0)
            {
                GameCanvas.startOKDlg("Nhập khoảng nghỉ từ 0 giây trở lên.");
                return;
            }
            if (requestedUses < 1 || requestedUses > QuantityInBag())
            {
                GameCanvas.startOKDlg("Số vật phẩm trong túi đã thay đổi. Hãy chọn Auto sử dụng lại.");
                onCancelChat();
                return;
            }
            Stop(ID);
            var auto = new AutoItem(ID, indexUI, seconds);
            auto.optionKey = optionKey;
            auto.remainingUses = requestedUses;
            listItemUses.Add(auto);
            GameScr.info1.addInfo("Auto sử dụng " + ItemTemplates.get((short)ID).name + ": " + requestedUses
                + " lần/cái, cách nhau " + seconds + " giây", 0);
            onCancelChat();
        }
        internal static void ResetTF(ChatTextField tf)
        {
            tf.strChat = "Chat"; tf.tfChat.name = "chat"; tf.to = "";
            tf.tfChat.setIputType(TField.INPUT_TYPE_ANY); tf.isShow = false; tf.parentScreen = GameScr.gI();
        }
        public void onCancelChat()
        {
            // The old mobile prompt closes automatically while step two is queued.
            if (!showNextPrompt) { setupStage = 0; requestedUses = 0; }
            ResetTF(ChatTextField.gI());
        }
    }
}
