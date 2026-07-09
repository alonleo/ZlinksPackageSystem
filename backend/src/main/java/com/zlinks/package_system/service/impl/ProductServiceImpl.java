package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.dto.CountResult;
import com.zlinks.package_system.entity.Product;
import com.zlinks.package_system.mapper.ProductMapper;
import com.zlinks.package_system.service.ProductService;
import org.springframework.stereotype.Service;

@Service
public class ProductServiceImpl extends ServiceImpl<ProductMapper, Product> implements ProductService {

    @Override
    public CountResult getCounts() {
        long total = count();
        long pending = count(new LambdaQueryWrapper<Product>().eq(Product::getStatus, "pending"));
        return new CountResult(total, pending);
    }
}
