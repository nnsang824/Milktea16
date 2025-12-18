// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
/* Đảm bảo nội dung luôn đẩy footer xuống đáy */
body {
  min-height: 100vh;
  background-color: #f8f9fa; /* Màu nền xám nhẹ cho toàn web */
}

/* Hover effect cho Card sản phẩm */
.card {
  transition: transform 0.2s ease-in-out, box-shadow 0.2s;
}

.card:hover {
  transform: translateY(-5px);
  box-shadow: 0 .5rem 1rem rgba(0,0,0,.15)!important;
}

/* Navbar link active */
.nav-link.active {
    color: #198754 !important; /* Màu xanh success */
    font-weight: bold;
}

/* Màu cho icon giỏ hàng */
.badge {
    font-size: 0.75rem;
}