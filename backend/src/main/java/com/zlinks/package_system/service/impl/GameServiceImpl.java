package com.zlinks.package_system.service.impl;

import com.baomidou.mybatisplus.core.conditions.query.LambdaQueryWrapper;
import com.baomidou.mybatisplus.extension.service.impl.ServiceImpl;
import com.zlinks.package_system.dto.CountResult;
import com.zlinks.package_system.entity.Game;
import com.zlinks.package_system.mapper.GameMapper;
import com.zlinks.package_system.service.GameService;
import org.springframework.stereotype.Service;

@Service
public class GameServiceImpl extends ServiceImpl<GameMapper, Game> implements GameService {

    @Override
    public CountResult getCounts() {
        long total = count();
        long pending = count(new LambdaQueryWrapper<Game>().eq(Game::getStatus, "pending"));
        return new CountResult(total, pending);
    }
}