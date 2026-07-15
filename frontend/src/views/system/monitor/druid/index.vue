<script setup lang="ts">
const druidUrl = '/druid/index.html'
</script>

<template>
  <div class="druid-container">
    <iframe :src="druidUrl" class="druid-iframe" />
  </div>
</template>

<style scoped>
.druid-container { height: calc(100vh - 160px); padding: 0; }
.druid-iframe { width: 100%; height: 100%; border: none; }
</style>