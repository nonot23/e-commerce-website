using System.Diagnostics.Tracing;
using Microsoft.AspNetCore.Mvc;
using StoreAPI.Data;
using StoreAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace StoreAPI.Controllers;

[Authorize] //Login แล้วถึงเข้าได้
[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    //สร้าง Object ของ ApplicationContext
    private readonly ApplicationDbContext _context;

    // IWebHostEnvironment ใช้สําหรับจัดการไฟล์และโฟลเดอร์
    // ContentRootPath คือ โฟลเดอร์ที่เก็บไฟล์ uploads
    // WebRootPath คือ เส้นทางถึงโฟลเดอร์ wwwroot เมื่อทำ UI ด้วย
    private readonly IWebHostEnvironment _env;

    public ProductController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    // GET: /api/Product/testconnectdb
    [AllowAnonymous] //ไม่ต้อง Login ก็สามารถเข้าได้
    [HttpGet("testconnectdb")]
    public void Testconnectdb()
    {
        //ถ้าเชื่อมต่อสำเร็จ จะแสดงข้อความ Success
        if (_context.Database.CanConnect())
        {
            Response.WriteAsync("Success Connect to Database");
        }
        else
        {
            Response.WriteAsync("Fail Connect to Database");
        }
    }
    // GET: /api/Product

    [HttpGet]
    public ActionResult<product> GetProducts()
    {
        //LINQ สำหรับดึงข้อมุล
        //var products =_context.products.ToList();
        // มีเงื่อนไข
        //var products = _context.products.Where(p => p.unit_price > 40000).ToList();
        // join ข้อมูล
        var products = _context.products
            .Join(
                _context.categories,
                p => p.category_id,
                c => c.category_id,
                (p, c) => new
                {
                    p.product_id,
                    p.product_name,
                    p.unit_price,
                    p.unit_in_stock,
                    c.category_name
                }
            ).ToList();
        return Ok(products);
    }

    //Get: /api/Product/{id}
        [Authorize]
        [HttpGet("{id}")]
        public async Task<ActionResult<product>> GetProduct(int id)
        {
            var product = await _context.products.FindAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

    // POST: /api/Product
    [HttpPost]
    public async Task<ActionResult<product>> PostProduct([FromForm] product product, IFormFile? image)
    {
                // เพิ่มข้อมูลลงในตาราง Products
        _context.products.Add(product);

        // ตรวจสอบว่ามีการอัพโหลดไฟล์รูปภาพหรือไม่
        if(image != null){
            // กำหนดชื่อไฟล์รูปภาพใหม่
            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);

            // บันทึกไฟล์รูปภาพ
            // string uploadFolder = Path.Combine(_env.ContentRootPath, "uploads");

            string uploadFolder = Path.Combine(_env.WebRootPath!, "uploads");

            // ตรวจสอบว่าโฟลเดอร์ uploads มีหรือไม่
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            using (var fileStream = new FileStream(Path.Combine(uploadFolder, fileName), FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }

            // บันทึกชื่อไฟล์รูปภาพลงในฐานข้อมูล
            product.product_picture = fileName;
        }

        _context.SaveChanges();

        // ส่งข้อมูลกลับไปให้ผู้ใช้
        return Ok(product);


    }

    // PUT: /api/Product/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> PutProduct(int id, product product)
    {
        if (id != product.product_id) return BadRequest();
        _context.Entry(product).State = EntityState.Modified;
        await _context.SaveChangesAsync();
        return Ok(product);
    }

    //DELETE: /api/Product/{id}
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        var product = await _context.products.FindAsync(id);
        if (product == null) return NotFound();
        _context.products.Remove(product);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
