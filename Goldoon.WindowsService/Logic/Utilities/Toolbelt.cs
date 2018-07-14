using System.Collections.Generic;
using System.Linq;
using Telegram.Bot.Types.InlineKeyboardButtons;

namespace Goldoon.Logic.Utilities
{
    class Toolbelt
    {

        public static int GetUserLastStatus(string Token)
        {
            using (WindowsService.Access.GoldoonEntities db = new WindowsService.Access.GoldoonEntities())
            {
                var _getUser = db.Users.Where(a => a.Token == Token).FirstOrDefault();
                if (_getUser != null)
                    return _getUser.Last_Info_Status == null ? EnumLastActionStatus.None : (int)_getUser.Last_Info_Status;
                else
                    return EnumLastActionStatus.None;

            }
        }

        public static int GetEditProductLastStatus(string Token)
        {
            using (WindowsService.Access.GoldoonEntities db = new WindowsService.Access.GoldoonEntities())
            {
                var _getUser = db.Users.Where(a => a.Token == Token).FirstOrDefault();
                if (_getUser != null)
                    return _getUser.Last_Edit_Status == null ? EnumEditProductStatus.None : (int)_getUser.Last_Edit_Status;
                else
                    return EnumEditProductStatus.None;

            }
        }

        public static int GetInsertProductStatus(string Token)
        {
            using (WindowsService.Access.GoldoonEntities db = new WindowsService.Access.GoldoonEntities())
            {
                var _getUser = db.Users.Where(a => a.Token == Token).FirstOrDefault();
                if (_getUser != null)
                    return _getUser.Insert_Product_Mode == null ? EnumInsertModeStatus.No : (int)_getUser.Insert_Product_Mode;
                else
                    return EnumInsertModeStatus.No;

            }
        }


        public static string[] EnumCommandSplitter(string InputEnum)
        {
            string[] arr = { "" };
            arr = InputEnum.Split('-', '_');
            if (arr.Length > 1)
                return arr;
            else
                return arr;
        }

        public static InlineKeyboardButton[][] GetInlineKeyboard(int column, int row, List<DynamicButtons> stringArray)
        {
            var keyboardInline = new InlineKeyboardButton[row][];
            var keyboardButtons = new InlineKeyboardButton[stringArray.Count];
            for (var i = 0; i < stringArray.Count; i++)
            {
                keyboardButtons[i] = InlineKeyboardButton.WithCallbackData(stringArray[i].Text, stringArray[i].Command);
            }
            for (int i = 0; i < row; i++)
            {
                keyboardInline[i] = keyboardButtons.Skip(i * column).Take(column).ToArray();
            }
            return keyboardInline;
        }
        public class DynamicButtons
        {
            public string Command { get; set; }
            public string Text { get; set; }
        }
    }
}
