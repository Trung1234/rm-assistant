// Ba tiện ích nhỏ. Giao diện là Razor Pages render phía server nên trang vẫn dùng được
// đầy đủ khi tắt JavaScript — không có gì ở đây là điều kiện để gửi hay phê duyệt.
(function () {
    'use strict';

    // 1. Cuộn xuống lượt mới nhất.
    var messages = document.getElementById('messages');
    if (messages) {
        messages.scrollTop = messages.scrollHeight;
    }

    // 2. Enter gửi, Shift+Enter xuống dòng.
    var box = document.getElementById('messageBox');
    var composer = document.getElementById('composer');
    if (box && composer) {
        box.addEventListener('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) {
                e.preventDefault();
                if (box.value.trim().length > 0) {
                    composer.requestSubmit();
                }
            }
        });
    }

    // 3. Khoá nút sau khi bấm. Một lượt agent mất vài giây, và bấm "Phê duyệt" hai lần
    //    là đúng cái tình huống mà idempotency key sinh ra để chặn — chặn sớm ở đây thì
    //    RM không phải nhìn thấy lỗi IDEMPOTENCY_CONFLICT chỉ vì tay nhanh.
    document.querySelectorAll('form').forEach(function (form) {
        form.addEventListener('submit', function () {
            form.querySelectorAll('[data-busy]').forEach(function (btn) {
                setTimeout(function () {
                    btn.disabled = true;
                    if (!btn.dataset.originalText) {
                        btn.dataset.originalText = btn.textContent;
                        btn.textContent = 'Đang xử lý…';
                    }
                }, 0);
            });
        });
    });
})();
