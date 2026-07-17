package com.zlinks.package_system.service;

import java.util.List;
import java.util.Map;

public interface WhitePackService {

    List<Map<String, String>> preview(String projectPath);

    List<Map<String, String>> apply(String projectPath);
}
