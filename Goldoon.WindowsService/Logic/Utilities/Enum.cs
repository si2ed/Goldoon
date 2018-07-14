namespace Goldoon.Logic.Utilities
{
    public class EnumValue
    {
        public string Command { get; set; }
        public string Text { get; set; }
        public string CommandBase { get; set; }
    }
    class Enum
    {
        public static EnumValue Product = new EnumValue() { CommandBase = "Products", Command = "Products", Text = "لیست محصولات" };
        public static EnumValue Home = new EnumValue() { CommandBase = "Home", Command = "Home", Text = "بازگشت به صفحه اصلی" };
        public static EnumValue Cart = new EnumValue() { CommandBase = "Cart", Command = "Cart", Text = "مشاهده سبد خرید" };
        public static EnumValue AddtoCart = new EnumValue() { CommandBase = "AddtoCart", Command = "AddtoCart-{0}", Text = "افزودن به سبد خرید" };
        public static EnumValue AddtoCartWithAmount = new EnumValue() { CommandBase = "AddtoCartWithAmount", Command = "AddtoCartWithAmount-{0}-{1}", Text = "" };
        public static EnumValue DeleteCartItem = new EnumValue() { CommandBase = "/DeleteCartItem", Command = "/DeleteCartItem_{0}", Text = "" };
        public static EnumValue ConfirmCart = new EnumValue() { CommandBase = "ConfirmCart", Command = "ConfirmCart", Text = "تایید سفارش" };
        public static EnumValue CategoryList = new EnumValue() { CommandBase = "CategoryList", Command = "CategoryList", Text = "لیست دسته بندی ها" };
        public static EnumValue CategoryProducts = new EnumValue() { CommandBase = "CategoryProducts", Command = "CategoryProducts-{0}", Text = "" };
        public static EnumValue OpenOrders = new EnumValue() { CommandBase = "OpenOrders", Command = "OpenOrders", Text = "مشاهده سفارشات" };
        public static EnumValue CompleteOrder = new EnumValue() { CommandBase = "/CompleteOrder", Command = "/CompleteOrder_{0}", Text = "" };
        public static EnumValue CancelOrder = new EnumValue() { CommandBase = "/CancelOrder", Command = "/CancelOrder_{0}", Text = "" };
        public static EnumValue EditProduct = new EnumValue() { CommandBase = "/EditProduct", Command = "/EditProduct_{0}", Text = "ویرایش محصول" };
        public static EnumValue EditProductAttributes = new EnumValue() { CommandBase = "/EditProductAttributes", Command = "/EditProductAttributes_{0}_{1}", Text = "" };
        public static EnumValue CancelEditProduct = new EnumValue() { CommandBase = "/CancelEditProduct", Command = "/CancelEditProduct", Text = "انصراف از ویرایش" };
        public static EnumValue AddProduct = new EnumValue() { CommandBase = "/AddProduct", Command = "/AddProduct", Text = "اضافه کردن محصول" };
        public static EnumValue PayOnDelivery = new EnumValue() { CommandBase = "/PayOnDelivery", Command = "/PayOnDelivery", Text = "پرداخت در محل" };
        public static EnumValue PayNow = new EnumValue() { CommandBase = "/PayNow", Command = "/PayNow", Text = "پرداخت اینترنتی" };
    }



    public static class EnumStatus
    {
        public static int Pending = 1;
        public static int Sent = 2;
        public static int Cancel = 3;
        public static int Paid = 4;
    }

    public static class EnumPaymentStatus
    {
        public static int Pending = 1;
        public static int Done = 2;
    }
    public static class EnumLastActionStatus
    {
        public static int None = 0;
        public static int WaitForAddress = 1;
        public static int WaitForMobile = 2;
    }

    public static class EnumInsertModeStatus
    {
        public static int No = 0;
        public static int Yes = 0;
    }
    public static class EnumEditProductStatus
    {
        public static int None = 0;
        public static int Name = 1;
        public static int Description = 2;
        public static int Image = 3;
        public static int Price = 4;
        public static int Quantity = 5;
        public static int Category = 6;
    }


}
