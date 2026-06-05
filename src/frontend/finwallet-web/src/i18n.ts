import { createI18n } from 'vue-i18n';

const messages = {
  en: {
    menu: {
      navigation: 'Navigation Menu',
      adminPanel: 'Admin Control Panel',
      dashboard: 'Dashboard',
      myWallet: 'My Wallet',
      topUp: 'Top-Up Wallet',
      sendMoney: 'Send Money',
      history: 'Transaction History',
      profile: 'My Profile',
      manageUsers: 'Manage Users',
      manageWallets: 'Manage Wallets',
      systemTransactions: 'System Transactions',
      auditLogs: 'Audit Logs',
      logout: 'Logout'
    },
    common: {
      refresh: 'Refresh',
      status: 'Status',
      lyd: 'LYD'
    },
    dashboard: {
      welcome: 'Welcome back, {name}!',
      subtitle: 'Here is a quick overview of your digital wallet.',
      activeBalance: 'Active Balance',
      walletNumber: 'Wallet Number',
      generating: 'Generating...',
      quickActions: 'Quick Actions',
      recentTransactions: 'Recent Transactions',
      viewAll: 'View All',
      noTransactions: 'No recent transactions found.',
      makeFirst: 'Make your first transfer',
      frozenAlert: 'Your Wallet is Frozen',
      frozenReason: 'Reason: {reason}. Outgoing and incoming transfers are temporarily blocked.'
    },
    wallet: {
      title: 'My Wallet',
      subtitle: 'Manage your digital account details and check ledger status',
      spec: 'Digital Account Spec',
      availableBalance: 'Available Ledger Balance',
      currencyUnit: 'Currency Unit',
      libyanDinar: 'Libyan Dinar (LYD)',
      precisionLimit: 'Precision Limit',
      decimalPlaces: '3 Decimal Places',
      manageBalance: 'Manage Balance',
      bankTopUp: 'Bank Top-Up',
      p2pTransfer: 'Peer-To-Peer Transfer',
      recentVolumes: 'Recent Activity Volumes',
      topUpVolume: 'Top-Up volume',
      transferVolume: 'Transfer volume',
      viewAllTx: 'View all transactions'
    },
    send: {
      title: 'Send Money',
      subtitle: 'Transfer funds securely to another active wallet instantly',
      recipientNum: 'Recipient Wallet Number',
      enterAmount: 'Enter Amount (LYD)',
      memo: 'Description / Memo (Optional)'
    },
    topup: {
      title: 'Top-Up Wallet',
      subtitle: 'Add funds to your wallet securely using our card processor',
      selectBank: 'Select Bank',
      cardNumber: '16-Digit Card Number',
      amount: 'Amount (LYD)',
      methodCard: 'Card Payment',
      methodBank: 'Bank Transfer',
      methodOnePay: 'onePay Transfer',
      methodLyPay: 'LyPay Transfer',
      accountNumber: 'Account Number (15 Digits)',
      onePayId: 'onePay Account Number',
      lyPayIban: 'LyPay IBAN (25 Digits)',
      bankNumberHint: 'Enter 15-digit bank account number',
      onePayHint: 'Enter registered onePay account number',
      lyPayHint: 'Enter 25-character Libyan IBAN starting with LY',
      successTitleTopup: 'Top-Up Request Submitted',
      successMessageTopup: 'Your top-up has been successfully processed!'
    }
  },
  ar: {
    menu: {
      navigation: 'القائمة الرئيسية',
      adminPanel: 'لوحة تحكم الإدارة',
      dashboard: 'لوحة المعلومات',
      myWallet: 'محفظتي الرقمية',
      topUp: 'إيداع وتعبئة الرصيد',
      sendMoney: 'تحويل الأموال',
      history: 'كشف حساب المعاملات',
      profile: 'الملف الشخصي',
      manageUsers: 'إدارة المستخدمين',
      manageWallets: 'إدارة المحافظ المالية',
      systemTransactions: 'حركة النظام المالية',
      auditLogs: 'سجلات المراقبة والتدقيق',
      logout: 'تسجيل الخروج من الحساب'
    },
    common: {
      refresh: 'تحديث البيانات',
      status: 'حالة العملية',
      lyd: 'د.ل'
    },
    dashboard: {
      welcome: 'مرحباً بك مجدداً، {name}!',
      subtitle: 'إليك ملخص شامل لعمليات محفظتك الرقمية.',
      activeBalance: 'الرصيد الفعلي المتاح',
      walletNumber: 'الرقم التعريفي للمحفظة',
      generating: 'جاري إنشاء الرقم...',
      quickActions: 'الخدمات السريعة',
      recentTransactions: 'أحدث الحركات المالية',
      viewAll: 'عرض كافة الحركات',
      noTransactions: 'لا توجد حركات مالية مسجلة مؤخراً في حسابك.',
      makeFirst: 'بادر بإجراء أول عملية تحويل مالي',
      frozenAlert: 'تم تجميد حساب محفظتك',
      frozenReason: 'سبب التجميد: {reason}. نعتذر، جميع عمليات الإرسال والاستقبال متوقفة مؤقتاً لحين المراجعة.'
    },
    wallet: {
      title: 'إدارة محفظتي',
      subtitle: 'التحكم الكامل في تفاصيل الحساب الرقمي ومراجعة حالة الرصيد المالي',
      spec: 'البيانات الأساسية للحساب الرقمي',
      availableBalance: 'الرصيد المالي المتاح في الحساب',
      currencyUnit: 'العملة المعتمدة',
      libyanDinar: 'الدينار الليبي (د.ل)',
      precisionLimit: 'دقة الفواصل العشرية',
      decimalPlaces: '٣ خانات عشرية',
      manageBalance: 'خيارات إدارة الرصيد',
      bankTopUp: 'إيداع رصيد عبر المصرف',
      p2pTransfer: 'تحويل فوري للأفراد (P2P)',
      recentVolumes: 'مؤشرات النشاط المالي الأخير',
      topUpVolume: 'إجمالي الإيداعات',
      transferVolume: 'إجمالي التحويلات الصادرة',
      viewAllTx: 'الاطلاع على جميع المعاملات'
    },
    send: {
      title: 'تحويل الأموال فورياً',
      subtitle: 'قم بإرسال الرصيد بكل أمان وسرعة إلى حساب محفظة أخرى مفعلة',
      recipientNum: 'الرقم التعريفي لمحفظة المستفيد',
      enterAmount: 'القيمة المراد تحويلها (بالدينار الليبي)',
      memo: 'ملاحظات إضافية حول التحويل (اختياري)'
    },
    topup: {
      title: 'تغذية رصيد المحفظة',
      subtitle: 'قم بزيادة رصيدك المالي بأمان تام عبر قنوات الدفع المتنوعة',
      selectBank: 'يرجى اختيار المصرف',
      cardNumber: 'رقم البطاقة المصرفية (١٦ رقماً)',
      amount: 'قيمة التعبئة المطلوبة (د.ل)',
      methodCard: 'بواسطة البطاقة المصرفية',
      methodBank: 'تحويل بنكي مباشر',
      methodOnePay: 'عبر خدمة ون باي (onePay)',
      methodLyPay: 'عبر خدمة لي باي (LyPay)',
      accountNumber: 'رقم الحساب المصرفي (١٥ رقماً)',
      onePayId: 'رقم حساب خدمة ون باي (onePay)',
      lyPayIban: 'رقم الحساب المصرفي الدولي (IBAN - ٢٥ رقماً)',
      bankNumberHint: 'يرجى كتابة رقم الحساب المصرفي بدقة',
      onePayHint: 'يرجى إدخال رقم حساب ون باي الخاص بك',
      lyPayHint: 'مثال: LY متبوعاً بـ ٢٣ رقماً',
      successTitleTopup: 'تم استلام طلب التعبئة بنجاح',
      successMessageTopup: 'تمت معالجة وإضافة الرصيد إلى محفظتك بنجاح!'
    }
  }
};

const savedLocale = localStorage.getItem('finwallet_locale') || 'en';

const i18n = createI18n({
  legacy: false,
  locale: savedLocale,
  fallbackLocale: 'en',
  messages
});

export default i18n;

